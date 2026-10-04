import { readFileSync } from "node:fs";
import i18next from "i18next";

const load = (lng, ns) => JSON.parse(readFileSync(new URL(`../locales/${lng}/${ns}.json`, import.meta.url), "utf8"));

const i18n = i18next.createInstance();
await i18n.init({
    lng: "en",
    fallbackLng: "en",
    ns: ["translation", "common"],
    defaultNS: "translation",
    interpolation: { escapeValue: false },
    resources: {
        en: { translation: load("en", "translation"), common: load("en", "common") },
        de: { translation: load("de", "translation"), common: load("de", "common") }
    }
});

const t = i18n.t.bind(i18n);
const deT = i18n.getFixedT("de");

const scenarios = {
    Simple: () => t("simple"),
    NestedKey: () => t("deep.nested.key"),
    Interpolation: () => t("greeting", { name: "Jane" }),
    MultipleInterpolations: () => t("multiple", { a: "one", b: "two", c: "three" }),
    Plural: () => t("item", { count: 5 }),
    ContextAndPlural: () => t("friend", { context: "male", count: 2 }),
    Nesting: () => t("nesting"),
    NumberFormat: () => t("price", { value: 1234.5 }),
    FallbackLanguage: () => deT("onlyEnglish"),
    Namespace: () => t("common:save"),
    ReturnObjects: () => t("menu", { returnObjects: true }),
    MissingKey: () => t("missing")
};

const expected = {
    Simple: "Hello world",
    Interpolation: "Hello Jane!",
    Plural: "5 items",
    ContextAndPlural: "2 boyfriends",
    Nesting: "Welcome to I18Next",
    NumberFormat: "Price: 1,234.5",
    FallbackLanguage: "Fallback text",
    Namespace: "Save",
    MissingKey: "missing"
};

for (const [name, value] of Object.entries(expected)) {
    if (scenarios[name]() !== value)
        throw new Error(`${name} returned ${scenarios[name]()} instead of ${value}`);
}

const measure = (fn) => {
    const deadline = process.hrtime.bigint() + 500_000_000n;
    let batch = 1000;

    while (process.hrtime.bigint() < deadline)
        for (let i = 0; i < batch; i++) fn();

    const startCalibration = process.hrtime.bigint();
    for (let i = 0; i < batch; i++) fn();
    const calibration = Number(process.hrtime.bigint() - startCalibration);
    batch = Math.max(1000, Math.round(batch * 100_000_000 / Math.max(calibration, 1)));

    const samples = [];

    for (let round = 0; round < 15; round++) {
        const start = process.hrtime.bigint();
        for (let i = 0; i < batch; i++) fn();
        samples.push(Number(process.hrtime.bigint() - start) / batch);
    }

    samples.sort((a, b) => a - b);

    return { median: samples[7], min: samples[0] };
};

const version = JSON.parse(readFileSync(new URL("node_modules/i18next/package.json", import.meta.url))).version;

console.log(`i18next ${version}, Node ${process.version}`);
console.log("| Scenario | Median | Fastest |");
console.log("|---|---:|---:|");

for (const [name, fn] of Object.entries(scenarios)) {
    const { median, min } = measure(fn);
    console.log(`| ${name} | ${median.toFixed(1)} ns | ${min.toFixed(1)} ns |`);
}
