export function getLanguage(storageKey, cookieName) {
    if (storageKey) {
        try {
            const language = localStorage.getItem(storageKey);

            if (language) {
                return language;
            }
        } catch {
        }
    }

    if (cookieName) {
        const prefix = cookieName + '=';
        const cookie = document.cookie.split('; ').find(c => c.startsWith(prefix));
        const match = cookie && /(?:^|\|)uic=([^|]+)/.exec(decodeURIComponent(cookie.substring(prefix.length)));

        if (match) {
            return match[1];
        }
    }

    return null;
}

export function setLanguage(language, dir, storageKey, cookieName) {
    document.documentElement.lang = language;
    document.documentElement.dir = dir;

    if (storageKey) {
        try {
            localStorage.setItem(storageKey, language);
        } catch {
        }
    }

    if (cookieName) {
        const value = encodeURIComponent(`c=${language}|uic=${language}`);
        const secure = location.protocol === 'https:' ? '; secure' : '';

        document.cookie = `${cookieName}=${value}; path=/; max-age=31536000; samesite=lax${secure}`;
    }
}
