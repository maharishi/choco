﻿// Copyright © 2011 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using app.registration;
    using logging;
    using SimpleInjector;

    /// <summary>
    ///   The inversion container
    /// </summary>
    public static class SimpleInjectorContainer
    {
        private static readonly Lazy<Container> _container = new Lazy<Container>(initialize);
        private static readonly IList<Type> _componentRegistries = new List<Type>();
        private const string REGISTER_COMPONENTS_METHOD = "RegisterComponents";

        /// <summary>
        ///   Add a component registry class to the container. 
        ///   Must have `public void RegisterComponents(Container container)`
        ///   and a parameterless constructor.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        public static void add_component_registry_class(Type componentType)
        {
            _componentRegistries.Add(componentType);
        }

        /// <summary>
        ///   Gets the container.
        /// </summary>
        public static Container Container { get { return _container.Value; } }

        /// <summary>
        ///   Initializes the container
        /// </summary>
        private static Container initialize()
        {
            var container = new Container();
            container.Options.AllowOverridingRegistrations = true;
            var originalConstructorResolutionBehavior = container.Options.ConstructorResolutionBehavior;
            container.Options.ConstructorResolutionBehavior = new SimpleInjectorContainerResolutionBehavior(originalConstructorResolutionBehavior);

            var binding = new ContainerBinding();
            binding.RegisterComponents(container);

            foreach (var componentRegistry in _componentRegistries)
            {
                load_component_registry(componentRegistry, container);
            }

#if DEBUG
            container.Verify();
#endif

            return container;
        }

        /// <summary>
        /// Loads a component registry for simple injector.
        /// </summary>
        /// <param name="componentRegistry">The component registry.</param>
        /// <param name="container">The container.</param>
        private static void load_component_registry(Type componentRegistry, Container container)
        {
            if (componentRegistry == null)
            {
                "chocolatey".Log().Error(
                    @"Type expected for registering components was null. Unable to provide 
 name due to it being null.");
                return;
            }
            try
            {
                object componentClass = Activator.CreateInstance(componentRegistry);

                componentRegistry.InvokeMember(
                    REGISTER_COMPONENTS_METHOD,
                    BindingFlags.InvokeMethod,
                    null,
                    componentClass,
                    new Object[] { container }
                    );
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Error(
                    ChocolateyLoggers.Important,
                    @"Error when registering components for '{0}':{1} {2}".format_with(
                        componentRegistry.FullName,
                        Environment.NewLine,
                        ex.Message
                        ));
            }
        }
    }
}
