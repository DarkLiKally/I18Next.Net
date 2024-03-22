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

    await pullUpstream();
    await upgradeVersion();
    await compileDependencies().then(compileMainProject).then(packLib);
};

const pullUpstream = async () => {
    return await runScript(`git pull upstream master`);
};

const upgradeVersion = async () => {
    const data = await fs.promises.readFile(pathToCsproj, {
        encoding: "utf-8",
    });

    const [match] = data.match(/<Version>\d+\.\d+\.\d+(\.\d+)*<\/Version>/);

    const [major, minor, patch, local = 0] = match
        .replace("<Version>", "")
        .replace("</Version>", "")
        .split(".")
        .map((e) => parseInt(e));

    var newVersion = [major, minor, patch, local + 1];

    const newData = data.replace(
        match,
        `<Version>${newVersion.join(".")}</Version>`
    );

    await fs.promises.writeFile(pathToCsproj, newData, { encoding: "utf-8" });

    return newVersion.join(".");
};

const compile = async (pathToCsproj) => {
    await runScript(`dotnet build --configuration Debug ${pathToCsproj}`);
};

const compileMainProject = async () => {
    return await compile(pathToCsproj);
};

const compileDependencies = async () => {
    for (const pathToDependency of pathsToDependencies) {
        await compile(pathToDependency);
        console.log(
            "----------------------------------------------------------"
        );
    }
};

const packLib = async () => {
    await runScript(`dotnet pack --configuration Debug ${pathToCsproj}`);
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
