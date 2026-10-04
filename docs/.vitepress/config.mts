import { defineConfig } from "vitepress";

const projectName = process.env.CI_PROJECT_NAME;

export default defineConfig({
    title: "I18Next.Net",
    description: "A port of i18next for .NET",
    base: process.env.DOCS_BASE ?? (projectName ? `/${projectName}/` : "/"),
    cleanUrls: true,
    markdown: {
        config(md) {
            const codeInline = md.renderer.rules.code_inline!;
            md.renderer.rules.code_inline = (...args) => codeInline(...args).replace("<code", "<code v-pre");
        }
    },
    lastUpdated: true,
    themeConfig: {
        nav: [
            { text: "Guide", link: "/guide/getting-started" },
            { text: "Plugins", link: "/plugins/backends" },
            { text: "Reference", link: "/reference/parity" }
        ],
        sidebar: [
            {
                text: "Guide",
                items: [
                    { text: "Getting started", link: "/guide/getting-started" },
                    { text: "Translation files", link: "/guide/translation-files" },
                    { text: "Translating", link: "/guide/translating" },
                    { text: "Formatting", link: "/guide/formatting" },
                    { text: "Plurals", link: "/guide/plurals" },
                    { text: "Language detection", link: "/guide/language-detection" },
                    { text: "Post processors", link: "/guide/post-processors" },
                    { text: "Missing keys and logging", link: "/guide/missing-keys-and-logging" }
                ]
            },
            {
                text: "Plugins",
                items: [
                    { text: "Backends", link: "/plugins/backends" },
                    { text: "Interpolators", link: "/plugins/interpolators" }
                ]
            },
            {
                text: "Integrations",
                items: [
                    { text: "Dependency injection", link: "/integrations/dependency-injection" },
                    { text: "Source generator", link: "/integrations/source-generator" }
                ]
            },
            {
                text: "Reference",
                items: [
                    { text: "Feature parity", link: "/reference/parity" },
                    { text: "Performance", link: "/reference/performance" },
                    { text: "Samples", link: "/reference/samples" },
                    { text: "Breaking changes", link: "/reference/breaking-changes" },
                    { text: "Development", link: "/reference/development" }
                ]
            }
        ],
        socialLinks: [{ icon: "github", link: "https://github.com/DarkLiKally/I18Next.Net" }],
        search: { provider: "local" },
        editLink: {
            pattern: "https://github.com/DarkLiKally/I18Next.Net/edit/develop/docs/:path"
        },
        footer: {
            message: "Released under the Apache License 2.0."
        }
    }
});
