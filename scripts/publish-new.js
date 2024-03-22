const path = require("path");
const fs = require("fs");
var childProcess = require("child_process");
const readline = require("readline");

// #region Config

const pathToCsproj = path.join(
    __dirname,
    "..",
    "src",
    "Localizer.Bundle",
    "Localizer.Bundle.csproj"
);

const pathsToDependencies = [
    path.join(__dirname, "..", "src", "Localizer", "Localizer.csproj"),
    path.join(
        __dirname,
        "..",
        "src",
        "Localizer.Abstractions",
        "Localizer.Abstractions.csproj"
    ),
    path.join(
        __dirname,
        "..",
        "src",
        "Localizer.AspNetCore",
        "Localizer.AspNetCore.csproj"
    ),
    path.join(
        __dirname,
        "..",
        "src",
        "Localizer.Extensions",
        "Localizer.Extensions.csproj"
    ),
    path.join(
        __dirname,
        "..",
        "src",
        "Localizer.PolyglotJs",
        "Localizer.PolyglotJs.csproj"
    ),
];

const pathNugetConfig = path.join(__dirname, "..", "..", "nuget.config");

// #endregion

const main = async () => {
    if (process.argv.length !== 3)
        console.log("use: node publish-new.js <patch|minor|major>");
    const newFeatAnswer = await getNewFeatAnswer();

    await pullUpstream();

    const newVersion = await upgradeVersion();

    await compileDependencies().then(compileMainProject).then(packLib);

    await publishToNugget(newVersion);

    await commitAndPush(newVersion, newFeatAnswer);
};

const getNewFeatAnswer = async () => {
    return new Promise((resolve) => {
        const rl = readline.createInterface({
            input: process.stdin,
            output: process.stdout,
        });

        rl.question("Qual a nova feat? ", (answer) => {
            rl.close();
            resolve(answer);
        });
    });
};

const pullUpstream = async () => {
    return await runScript(`git pull upstream master`);
};

const upgradeVersion = async () => {
    const upgrade = process.argv[2];
    if (!upgrade) throw new Error("upgrade not found");
    if (upgrade !== "patch" && upgrade !== "minor" && upgrade !== "major")
        throw new Error("invalid upgrade");

    const data = await fs.promises.readFile(pathToCsproj, {
        encoding: "utf-8",
    });

    const [match] = data.match(/<Version>\d+\.\d+\.\d+(\.\d+)*<\/Version>/);

    const [major, minor, patch] = match
        .replace("<Version>", "")
        .replace("</Version>", "")
        .split(".")
        .map((e) => parseInt(e));

    const newVersion = [major, minor, patch];

    const upgradeIndex = upgrade === "patch" ? 2 : upgrade === "minor" ? 1 : 0;

    newVersion[upgradeIndex] = newVersion[upgradeIndex] + 1;
    for (let i = upgradeIndex + 1; i < 3; i++) newVersion[i] = 0;

    const newData = data.replace(
        match,
        `<Version>${newVersion.join(".")}</Version>`
    );

    await fs.promises.writeFile(pathToCsproj, newData, { encoding: "utf-8" });

    return newVersion.join(".");
};

const compile = async (pathToCsproj) => {
    await runScript(`dotnet build --configuration Release ${pathToCsproj}`);
};

const compileMainProject = async () => {
    return await compile(pathToCsproj);
};

const compileDependencies = async () => {
    for (const pathToDependency of pathsToDependencies) {
        await compile(pathToDependency);
    }
};

const packLib = async () => {
    await runScript(`dotnet pack --configuration Release ${pathToCsproj}`);
};

const publishToNugget = async (newVersion) => {
    const pathNupkg = path.resolve(
        __dirname,
        "..",
        "src",
        "Localizer.Bundle",
        "bin",
        "Release",
        `Localizer.Bundle.${newVersion}.nupkg`
    );

    const windowsNuggetCommand = `nuget push ${pathNupkg} -Source "https://nuget.pkg.github.com/optimuz-optz/index.json" -ConfigFile ${pathNugetConfig}`;
    const linuxNuggetCommand = `dotnet nuget push ${pathNupkg} --source "https://nuget.pkg.github.com/optimuz-optz/index.json" -k $NUGET_KEY`;

    const nuggetCommand =
        process.platform === "linux"
            ? linuxNuggetCommand
            : windowsNuggetCommand;

    await runScript(nuggetCommand);
};

const commitAndPush = async (newVersion, newFeatAnswer) => {
    await runScript(`git add .`);
    await runScript(
        `git commit -m "feat: new version ${newVersion} ${newFeatAnswer}"`
    );
};

const runScript = async (scriptPath) => {
    var process = childProcess.spawn(scriptPath, { shell: true });
    return await new Promise((accept, reject) => {
        process.stdout.on("data", (data) => {
            console.log(`${data}`);
        });

        process.stderr.on("data", (data) => {
            console.error(`${data}`);
        });

        process.on("error", (error) => {
            console.error(`${error.message}`);
        });

        process.on("close", (code) => {
            console.log(`child process exited with code ${code}`);
            if (parseInt(code) === 0) accept();
            else throw new Error(`child process exited with code ${code}`);
        });
    });
};

main();
