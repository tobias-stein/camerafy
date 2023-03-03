import backend from '../../services/backend'

class TranslationService
{
    async listLanguages()
    {
        const response = await backend.get('i18n/languages/');
        return response.results;
    }

    async listTranslationKeysChunked(page=1)
    {
        const response = await backend.get(`i18n/translationkeys/?page=${page}`);
        return response.results;
    }

    async listTranslationKeysProgress()
    {
        return await backend.get('i18n/translationkeys/progress/');
    }

    async listLanguagesChunked(page=1)
    {
        const response = await backend.get(`i18n/languages/?page=${page}`);
        return response.results;    
    }

    async listLanguagesProgress()
    {
        return await backend.get('i18n/languages/progress/');
    }

    async addTranslationKey(newKey)
    {
        return await backend.post('i18n/translationkeys/', {key: newKey})
    }

    async updateTranslationKey(newKey, id)
    {
        return await backend.patch(`i18n/translationkeys/${id}/`, {key: newKey})
    }

    async updateTranslationKeyType(newType, id)
    {
        return await backend.patch(`i18n/translationkeys/${id}/`, {type: newType})
    }

    async deleteTranslationKey(id)
    {
        return await backend.delete(`i18n/translationkeys/${id}`)
    }

    async createOrUpdateLanguage(lan_code, lan_name, reg_code, reg_name, lan_id = -1)
    {
        // update
        if(lan_id !== -1)
        {
            return await backend.patch(`i18n/languages/${lan_id}/`, {lan_code: lan_code, lan_name: lan_name, reg_code: reg_code, reg_name: reg_name});

        }
        // create
        else
        {
            return await backend.post('i18n/languages/', {lan_code: lan_code, lan_name: lan_name, reg_code: reg_code, reg_name: reg_name});
        }
    }

    async deleteLanguage(id)
    {
        return await backend.delete(`i18n/languages/${id}/`);
    }

    async getTranslation(language_code)
    {
        return await backend.get(`i18n/translations/?language=${language_code}&flat=true`);
    }

    async revisionCheck(translationId, newRevision)
    {
        const response = await backend.get(`i18n/translations/${translationId}/`); 
        if(response === null || response.revision >= newRevision)
            return false;
        
        return true;
    }

    async createTranslation(translation_obj, tranlationKeyId, languageId)
    {
        return await backend.post(`i18n/translations/`, 
        {
            translation: translation_obj.translation,
            revision: 1,
            translation_key: tranlationKeyId,
            language: languageId
        });
    }

    async updateTranslation(translation_obj)
    {
        const newRevision = translation_obj.revision + 1;
        const _revisionCheck = await this.revisionCheck(translation_obj.id, newRevision);
        if(!_revisionCheck)
            return -1;

        return await backend.patch(`i18n/translations/${translation_obj.id}/`, 
        {
            translation: translation_obj.translation,
            revision: newRevision
        });
    }

    async deleteTranslation(id)
    {
        return await backend.delete(`i18n/translations/${id}/`);
    }
}

export default new TranslationService();