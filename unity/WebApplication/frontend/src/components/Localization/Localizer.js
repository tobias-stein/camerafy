import backend from '../../services/backend'

class Localizer
{
    constructor()
    {
        this._languageCache = {};
        this.default_language = "en-US";
        this.active_language = {};
        this.supported_languages = [];

        // get all supported languages from backend
        this.loadLanguages();
    }

    async _setLanguage(language)
    {
        if(this._languageCache[language.lan_code] === undefined)
        {
            const T = await backend.get(`i18n/translations/?language=${language.lan_code}&flat=true`)
            this._languageCache[language.lan_code] = T[language.lan_code];
        }

        this.active_language = language;
    }

    async activateLanguageByCode(code)
    {
        const language_index = this.supported_languages.map(lang => { return lang.lan_code; }).indexOf(this.default_language);
        if(!language_index || language_index == -1)
        {
            console.log(`Trying to active a language with code ${code}, which is not supported. Language change skipped.`);
            return;
        }

        await this._setLanguage(this.supported_languages[language_index]);
    }

    async activateLanguageByIndex(index)
    {
        if(!index || (index < 0 || index >= this.supported_languages.length))
        {
            console.log(`Trying to active a language with index ${index}, which is out of range. Language change skipped.`);
            return;
        }

        this._setLanguage(this.supported_languages[index]);
    }

    async loadLanguages()
    {
        const __listLanguagesChunked = async function(page=1)
        {
            const response = await backend.get(`i18n/languages/?page=${page}`);
            return response.results;    
        }

        this.supported_languages = [];

        for(var chunk = 1;;chunk++)
        {
            const languages = await __listLanguagesChunked(chunk);
            // if no more data bailout here
            if(languages === undefined)
                break;

            this.supported_languages = this.supported_languages.concat(languages);
        }

        // Sort langauge selection descending by language name
        this.supported_languages.sort(function (a, b) { return ('' + a.lan_name).localeCompare(b.lan_name); });

        // select default language
        this.activateLanguageByCode(this.default_language);
    }

    get activeLanguageIndex()
    {
        return this.supported_languages.map(lang => { return lang.lan_code; }).indexOf(this.active_language.lan_code);
    }
}

const LocalizerInstance = new Localizer();
export default LocalizerInstance;

// Create a global function $t callabel everywhere
window.$t = function(translationKey)
{
    if(LocalizerInstance === undefined)
    {
        return "null";
    }

    if(LocalizerInstance._languageCache[LocalizerInstance.active_language.lan_code] === this.undefined)
    {
        return "undef";
    }

    const translation = LocalizerInstance._languageCache[LocalizerInstance.active_language.lan_code][translationKey];

    if(translation === this.undefined || translation.id == -1)
    {
        return "???";
    }

    return translation.translation;
}
