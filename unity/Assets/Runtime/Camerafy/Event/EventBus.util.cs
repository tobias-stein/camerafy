using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace Camerafy.Event
{
    /// <summary>
    /// Additional helper methods.
    /// </summary>
    public partial class EventBus
    {
        /// <summary>
        /// This will gather all instances for any CamerafyCommand annotated method.
        /// The map is later used by the Eventing.Dispatcher.
        /// </summary>
        private Dictionary<string, object> GetInstanceMethodMap(Participant InParticipant)
        {
            Dictionary<string, object> InstanceMethodMap = new Dictionary<string, object>();

            List<Type> Ignore = new List<Type>()
            {
                typeof(UnityEngine.Transform),
                typeof(UnityEngine.RectTransform),
                typeof(UnityEngine.Camera),
                typeof(UnityEngine.Canvas),
                typeof(UnityEngine.CanvasGroup),
                typeof(UnityEngine.CanvasRenderer),
            };

            // Gets all Unity components attached to the GameObject hierarchy
            var UnityComponents = this.GetAllComponents(InParticipant.gameObject, Ignore);
            // Get all class objects from these components
            var AllClassObjects = this.GetAllObjects(UnityComponents);
            AllClassObjects.Add(InParticipant);

            foreach (object instance in AllClassObjects)
            {
                // get all methods for client to server calls
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
                foreach (MethodInfo method in instance.GetType().GetMethods(flags).Where(x => { var attr = x.GetCustomAttribute<CamerafyEventAttribute>(); return (attr != null && (attr.Properties.HasFlag(CamerafyEventProperty.Client))) ? true : false; }))
                {
                    string MethodAddress = EventLibrary.CalculateMethodAddress(method);

                    if (InstanceMethodMap.ContainsKey(MethodAddress))
                    {
                        // do not add twice!
                        continue;
                    }

                    InstanceMethodMap.Add(MethodAddress, instance);
                }
            }

            return InstanceMethodMap;
        }

        /// <summary>
        /// Get all object instances and their descents from all components.
        /// </summary>
        /// <param name="UnityComponents"></param>
        /// <returns></returns>
        private List<object> GetAllObjects(List<Component> UnityComponents)
        {
            List<object> Objects = new List<object>();

            foreach (var c in UnityComponents)
            {
                this.GetAllObjects(c, ref Objects);
            }

            return Objects;
        }

        private void GetAllObjects(object Instance, ref List<object> SeenInstances)
        {
            List<object> Objects = new List<object>();
            Type InstanceType = Instance.GetType();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

            // get all fields and properties which are in 'Camerafy.*' namespace
            var CamerafyMember = InstanceType
                .GetFields(flags).Cast<MemberInfo>()
                .Concat(InstanceType.GetProperties(flags))
                .Where(x => {

                    var MemberType = x.MemberType == MemberTypes.Field ? (x as FieldInfo).FieldType : (x as PropertyInfo).PropertyType;

                    // Ignore if not class
                    if (!MemberType.IsClass)
                        return false;

                    // Ignore arrays
                    if (MemberType.IsArray)
                        return false;

                    // Ignore enums
                    if (MemberType.IsEnum)
                        return false;

                    // Ignore abstract
                    if (MemberType.IsAbstract)
                        return false;

                    // Ignore non 'Camerafy.' objects
                    if (!MemberType.FullName.StartsWith("Camerafy"))
                        return false;

                    return true;
                })
                .ToList();

            // grab object instances, we have not seen yet
            foreach (var m in CamerafyMember)
            {
                object instance = null;
                if (m.MemberType == MemberTypes.Field)
                {
                    instance = (m as FieldInfo).GetValue(Instance);
                }
                else
                {
                    instance = (m as PropertyInfo).GetValue(Instance);
                }

                if (instance != null && !SeenInstances.Contains(instance))
                {
                    // mark this instance as seen
                    SeenInstances.Add(instance);
                    Objects.Add(instance);
                }
            }

            // recursevely check these member
            foreach (var child in Objects)
            {
                this.GetAllObjects(child, ref SeenInstances);
            }
        }

        /// <summary>
        /// Returns all Unity Components attached to the root GameObject.
        /// </summary>
        /// <param name="Root"></param>
        /// <param name="Ignore"></param>
        /// <returns></returns>
        private List<Component> GetAllComponents(GameObject Root, List<Type> Ignore)
        {
            List<Component> Components = new List<Component>();

            foreach (Component component in Root.GetComponents(typeof(Component)))
            {
                // skip this component if its in 'ignore' list
                if (Ignore.Contains(component.GetType()))
                    continue;

                Components.Add(component);
            }

            for (int i = 0; i < Root.transform.childCount; ++i)
            {
                GameObject Child = Root.transform.GetChild(i).gameObject;
                Components.AddRange(this.GetAllComponents(Child, Ignore));
            }

            return Components;
        }
    }
}
