import Vue from 'vue'
import User from '../services/user'

class CamerafyExtensions
{
    constructor()
    {
        const _this = this;

        this.extensions = new Array();

        // source: https://github.com/chrisvfritz/vue-enterprise-boilerplate/blob/master/src/components/_globals.js

        const requireComponent = require.context(
            // Look for files in the current directory
            '.',
            // Do not look in subdirectories
            true,
            // look for all .vue files
            /\w+\.vue$/
        )
        // For each matching file name...
        requireComponent.keys().forEach((fileName) => {

            // Get the component config
            const componentConfig = requireComponent(fileName)

            // Get the PascalCase version of the component name
            const pathElements = fileName.split('/');

            const extensionsRootDir = pathElements[1];
            const extensionsRootVue = pathElements[pathElements.length - 1].replace(".vue", "");

            // only load root vue as extension
            if(extensionsRootVue !== extensionsRootDir)
                return;

            const orderIndex = (componentConfig.default.props.extensionOrderIndex !== undefined && componentConfig.default.props.extensionOrderIndex.default !== undefined ? componentConfig.default.props.extensionOrderIndex.default: 99);

            const componentName = `camerafyExtension-${orderIndex}-${extensionsRootVue}`;

            // Globally register the component
            Vue.component(componentName, componentConfig.default || componentConfig);

            _this.extensions.push({'name': componentName, 'config': componentConfig.default || componentConfig });
        })

        this.extensions.sort();
    }

    getEligibleExtensions() 
    { 
        const userAuthenticated = User.isLoggedIn;
        const user_permission_groups = User.permission_groups;

        const filterEligibleExtension = function(extension)
        {
            // if extension requires user to be authenticated
            if(extension.config.props.requires_authentication !== undefined && extension.config.props.requires_authentication.default === true)
            {
                if(userAuthenticated === false)
                    return false;
            }

            // if extension has no permission group assigned, anyone can use it
            if(extension.config.props.permission_group === undefined)
                return true;

            const extensionPErmission_group = extension.config.props.permission_group.default.toLowerCase();
            return user_permission_groups.find(elem => elem.toLowerCase() === extensionPErmission_group);
        }

        return this.extensions.filter(filterEligibleExtension).map(it => it.name).sort(); 
    }
}

export default new CamerafyExtensions();