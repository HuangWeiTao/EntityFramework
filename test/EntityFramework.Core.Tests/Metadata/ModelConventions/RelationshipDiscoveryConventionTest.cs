// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Metadata.ModelConventions;
using Xunit;

namespace Microsoft.Data.Entity.Tests.Metadata.ModelConventions
{
    public class RelationshipDiscoveryConventionTest
    {
        [Fact]
        public void Entity_type_is_discovered_through_private_unidirectional_nonCollection_navigation_when_no_PK_on_principal()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyDependent>(OneToManyPrincipal.IgnoreNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            Assert.Empty(entityBuilder.Metadata.Properties);
            Assert.Empty(entityBuilder.Metadata.ForeignKeys);
            Assert.Empty(entityBuilder.Metadata.Navigations);
            Assert.Equal(2, entityBuilder.Metadata.Model.EntityTypes.Count);
            
            var principalEntityType = entityBuilder.Metadata.Model.EntityTypes.Single(e => e.Type == typeof(OneToManyPrincipal));
            Assert.Empty(principalEntityType.Properties);
            Assert.Empty(principalEntityType.ForeignKeys);
            Assert.Empty(principalEntityType.Navigations);
        }

        [Fact]
        public void Entity_type_is_discovered_through_unidirectional_collection_navigation_when_no_PK_on_principal()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyPrincipal>(OneToManyDependent.IgnoreNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            Assert.Empty(entityBuilder.Metadata.Properties);
            Assert.Empty(entityBuilder.Metadata.ForeignKeys);
            Assert.Empty(entityBuilder.Metadata.Navigations);

            var principalEntityType = entityBuilder.Metadata.Model.EntityTypes.Single(e => e.Type == typeof(OneToManyPrincipal));
            Assert.Empty(principalEntityType.Properties);
            Assert.Empty(principalEntityType.ForeignKeys);
            Assert.Empty(principalEntityType.Navigations);
        }

        [Fact]
        public void Entity_type_is_not_discovered_if_ignored()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyDependent>();
            entityBuilder.ModelBuilder.Ignore(typeof(OneToManyPrincipal).FullName, ConfigurationSource.DataAnnotation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            Assert.Empty(entityBuilder.Metadata.Properties);
            Assert.Empty(entityBuilder.Metadata.ForeignKeys);
            Assert.Empty(entityBuilder.Metadata.Navigations);
            Assert.Equal(entityBuilder.Metadata.Type, entityBuilder.Metadata.Model.EntityTypes.Single().Type);
        }

        [Fact]
        public void Entity_type_is_not_discovered_if_navigation_is_ignored()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyDependent>();
            entityBuilder.Ignore(OneToManyDependent.NavigationProperty.Name, ConfigurationSource.DataAnnotation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            Assert.Empty(entityBuilder.Metadata.Properties);
            Assert.Empty(entityBuilder.Metadata.ForeignKeys);
            Assert.Empty(entityBuilder.Metadata.Navigations);
            Assert.Equal(entityBuilder.Metadata.Type, entityBuilder.Metadata.Model.EntityTypes.Single().Type);
        }

        [Fact]
        public void One_to_one_bidirectional_is_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToOnePrincipal>(ConfigureKeys);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            VerifyOneToOne(entityBuilder.Metadata.Model);

            new RelationshipDiscoveryConvention().Apply(entityBuilder.ModelBuilder.Entity(typeof(OneToOneDependent), ConfigurationSource.Convention));

            VerifyOneToOne(entityBuilder.Metadata.Model);
        }

        [Fact]
        public void One_to_many_unidirectional_is_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyDependent>(
                ConfigureKeys, OneToManyPrincipal.IgnoreNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            VerifyOneToManyDependent(entityBuilder, unidirectional: true);
        }

        [Fact]
        public void One_to_many_unidirectional_remains_unchanged_if_already_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyDependent>(
                ConfigureKeys, OneToManyPrincipal.IgnoreNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);
            var entityType = entityBuilder.Metadata;
            var fk = entityType.ForeignKeys.Single();
            Assert.Null(fk.IsRequired);
            Assert.False(fk.IsUnique.Value);

            fk.IsRequired = true;
            fk.IsUnique = true;

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            var newFk = entityType.ForeignKeys.Single();
            Assert.True(newFk.IsRequired.Value);
            Assert.True(newFk.IsUnique.Value);
        }

        [Fact]
        public void One_to_many_bidirectional_is_not_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyDependent>();

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            Assert.Empty(entityBuilder.Metadata.ForeignKeys);
            Assert.Empty(entityBuilder.Metadata.Navigations);
            Assert.Empty(entityBuilder.Metadata.Properties);
        }

        [Fact]
        public void Many_to_one_unidirectional_is_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyPrincipal>(
                ConfigureKeys, OneToManyDependent.IgnoreNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            VerifyOneToManyPrincipal(entityBuilder, unidirectional: true);
        }

        [Fact]
        public void Many_to_one_bidirectional_is_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<OneToManyPrincipal>(ConfigureKeys);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            VerifyOneToManyPrincipal(entityBuilder, unidirectional: false);
        }

        [Fact]
        public void Many_to_many_bidirectional_is_not_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<ManyToManyFirst>(ConfigureKeys);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            // TODO: remove discovered entity types if no relationship discovered
            //VerifyModelUnchanged(entityBuilder);
        }

        [Fact]
        public void Ambiguous_navigations_are_not_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<MultipleNavigationsFirst>(
                ConfigureKeys, MultipleNavigationsSecond.IgnoreCollectionNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            // TODO: remove discovered entity types if no relationship discovered
            //VerifyModelUnchanged(entityBuilder);
        }

        [Fact]
        public void Ambiguous_reverse_navigations_are_not_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<MultipleNavigationsSecond>(
                ConfigureKeys, MultipleNavigationsSecond.IgnoreCollectionNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            // TODO: remove discovered entity types if no relationship discovered
            //VerifyModelUnchanged(entityBuilder);
        }

        [Fact]
        public void Multiple_navigations_to_same_entity_type_are_discovered()
        {
            var entityBuilder = CreateInternalEntityBuilder<MultipleNavigationsFirst>(
                ConfigureKeys, MultipleNavigationsSecond.IgnoreCollectionNavigation, MultipleNavigationsSecond.IgnoreNonCollectionNavigation);

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            var model = (IModel)entityBuilder.Metadata.Model;
            Assert.Equal(2, model.EntityTypes.Count);
            var firstEntityType = model.EntityTypes.Single(e => e.Type == typeof(MultipleNavigationsFirst));
            var secondEntityType = model.EntityTypes.Single(e => e.Type == typeof(MultipleNavigationsSecond));

            Assert.Equal(2, firstEntityType.Properties.Count);
            Assert.Equal(1, firstEntityType.Keys.Count);
            var firstFK = firstEntityType.ForeignKeys.Single();
            Assert.False(firstFK.IsRequired);
            Assert.False(firstFK.IsUnique);
            Assert.Equal(
                new[] { MultipleNavigationsFirst.NonCollectionNavigationProperty.Name, MultipleNavigationsFirst.CollectionNavigationProperty.Name },
                firstEntityType.Navigations.Select(n => n.Name));

            Assert.Equal(2, secondEntityType.Properties.Count);
            Assert.Equal(1, secondEntityType.Keys.Count);
            var secondFK = firstEntityType.ForeignKeys.Single();
            Assert.False(secondFK.IsRequired);
            Assert.False(secondFK.IsUnique);
            Assert.Empty(secondEntityType.Navigations);
        }

        [Fact]
        public void Does_not_discover_nonNavigation_properties()
        {
            var entityBuilder = CreateInternalEntityBuilder<EntityWithNoValidNavigations>();

            new RelationshipDiscoveryConvention().Apply(entityBuilder);

            Assert.Empty(entityBuilder.Metadata.ForeignKeys);
            Assert.Empty(entityBuilder.Metadata.Navigations);
            Assert.Empty(entityBuilder.Metadata.Properties);
        }

        private class EntityWithNoValidNavigations
        {
            public int Id { get; set; }

            public static OneToManyDependent Static { get; set; }

            public OneToManyDependent WriteOnly
            {
                set { }
            }

            public OneToManyDependent ReadOnly
            {
                get { return null; }
            }

            public OneToManyDependent this[int index]
            {
                get { return null; }
                set { }
            }

            public MyStruct Struct { get; set; }
            public IInterface Interface { get; set; }
            public AbstractClass Abstract { get; set; }
        }

        private struct MyStruct
        {
            public int Id { get; set; }
        }

        private interface IInterface
        {
            int Id { get; set; }
        }

        private abstract class AbstractClass
        {
            public int Id { get; set; }
        }

        private static void VerifyOneToOne(IModel model)
        {
            Assert.Equal(2, model.EntityTypes.Count);
            var principalEntityType = model.EntityTypes.Single(e => e.Type == typeof(OneToOnePrincipal));
            var dependentEntityType = model.EntityTypes.Single(e => e.Type == typeof(OneToOneDependent));

            Assert.Equal(1, principalEntityType.Properties.Count);
            Assert.Equal(1, principalEntityType.Keys.Count);
            Assert.Empty(principalEntityType.ForeignKeys);
            Assert.Equal(OneToOnePrincipal.NavigationProperty.Name, principalEntityType.Navigations.Single().Name);

            Assert.Equal(1, dependentEntityType.Properties.Count);
            Assert.Equal(1, dependentEntityType.Keys.Count);
            var fk = dependentEntityType.ForeignKeys.Single();
            Assert.True(fk.IsRequired);
            Assert.True(fk.IsUnique);
            Assert.Equal(OneToOneDependent.NavigationProperty.Name, dependentEntityType.Navigations.Single().Name);
        }

        private static void VerifyOneToManyPrincipal(InternalEntityBuilder entityBuilder, bool unidirectional, bool dependentHasPK = true)
        {
            VerifyOneToMany(entityBuilder.Metadata.Model, dependentHasPK, hasNavigationToDependent: true, hasNavigationToPrincipal: !unidirectional);
        }

        private static void VerifyOneToManyDependent(InternalEntityBuilder entityBuilder, bool unidirectional, bool dependentHasPK = true)
        {
            VerifyOneToMany(entityBuilder.Metadata.Model, dependentHasPK, hasNavigationToDependent: !unidirectional, hasNavigationToPrincipal: true);
        }

        private static void VerifyOneToMany(
            IModel model,
            bool dependentHasPK,
            bool hasNavigationToDependent,
            bool hasNavigationToPrincipal)
        {
            Assert.Equal(2, model.EntityTypes.Count);
            var principalEntityType = model.EntityTypes.Single(e => e.Type == typeof(OneToManyPrincipal));
            var dependentEntityType = model.EntityTypes.Single(e => e.Type == typeof(OneToManyDependent));

            Assert.Equal(dependentHasPK ? 2 : 1, dependentEntityType.Properties.Count);
            Assert.Equal(dependentHasPK ? 1 : 0, dependentEntityType.Keys.Count);
            var fk = dependentEntityType.ForeignKeys.Single();
            Assert.False(fk.IsRequired);
            Assert.False(fk.IsUnique);
            if (hasNavigationToPrincipal)
            {
                Assert.Equal(OneToManyDependent.NavigationProperty.Name, dependentEntityType.Navigations.Single().Name);
            }
            else
            {
                Assert.Empty(dependentEntityType.Navigations);
            }

            Assert.Equal(1, principalEntityType.Properties.Count);
            Assert.Equal(1, principalEntityType.Keys.Count);
            Assert.Empty(principalEntityType.ForeignKeys);
            if (hasNavigationToDependent)
            {
                Assert.Equal(OneToManyPrincipal.NavigationProperty.Name, principalEntityType.Navigations.Single().Name);
            }
            else
            {
                Assert.Empty(principalEntityType.Navigations);
            }
        }

        private static InternalEntityBuilder CreateInternalEntityBuilder<T>(params Action<InternalEntityBuilder>[] onEntityAdded)
        {
            var modelBuilder = new InternalModelBuilder(new Model(), onEntityAdded == null ? null : new TestModelChangeListener(onEntityAdded));
            var entityBuilder = modelBuilder.Entity(typeof(T), ConfigurationSource.Convention);

            return entityBuilder;
        }

        private static void ConfigureKeys(InternalEntityBuilder entityBuilder)
        {
            entityBuilder.Key(new[] { "Id" }, ConfigurationSource.Convention);
        }

        private class TestModelChangeListener : IModelChangeListener
        {
            private readonly Action<InternalEntityBuilder>[] _onEntityAdded;

            public TestModelChangeListener(Action<InternalEntityBuilder>[] onEntityAdded)
            {
                _onEntityAdded = onEntityAdded;
            }

            public void OnEntityTypeAdded(InternalEntityBuilder entityBuilder)
            {
                foreach (var action in _onEntityAdded)
                {
                    action(entityBuilder);
                }
            }
        }

        public class OneToOnePrincipal
        {
            public static readonly PropertyInfo NavigationProperty =
                typeof(OneToOnePrincipal).GetProperty("OneToOneDependent", BindingFlags.Public | BindingFlags.Instance);

            public int Id { get; set; }
            public OneToOneDependent OneToOneDependent { get; set; }
        }

        public class OneToOneDependent
        {
            public static readonly PropertyInfo NavigationProperty =
                typeof(OneToOneDependent).GetProperty("OneToOnePrincipal", BindingFlags.Public | BindingFlags.Instance);

            public int Id { get; set; }
            public OneToOnePrincipal OneToOnePrincipal { get; set; }
        }

        public class OneToManyPrincipal
        {
            public static readonly PropertyInfo NavigationProperty =
                typeof(OneToManyPrincipal).GetProperty("OneToManyDependents", BindingFlags.Public | BindingFlags.Instance);

            public int Id { get; set; }

            public IEnumerable<OneToManyDependent> OneToManyDependents { get; set; }

            public static void IgnoreNavigation(InternalEntityBuilder entityBuilder)
            {
                if (entityBuilder.Metadata.Type == typeof(OneToManyPrincipal))
                {
                    entityBuilder.Ignore(NavigationProperty.Name, ConfigurationSource.DataAnnotation);
                }
            }
        }

        public class OneToManyDependent
        {
            public static readonly PropertyInfo NavigationProperty =
                typeof(OneToManyDependent).GetProperty("OneToManyPrincipal", BindingFlags.NonPublic | BindingFlags.Instance);

            public int Id { get; set; }

            private OneToManyPrincipal OneToManyPrincipal { get; set; }

            public static void IgnoreNavigation(InternalEntityBuilder entityBuilder)
            {
                if (entityBuilder.Metadata.Type == typeof(OneToManyDependent))
                {
                    entityBuilder.Ignore(NavigationProperty.Name, ConfigurationSource.DataAnnotation);
                }
            }
        }

        public class ManyToManyFirst
        {
            public static readonly PropertyInfo NavigationProperty =
                typeof(OneToManyPrincipal).GetProperty("ManyToManySeconds", BindingFlags.Public | BindingFlags.Instance);

            public int Id { get; set; }
            public IEnumerable<ManyToManySecond> ManyToManySeconds { get; set; }
        }

        public class ManyToManySecond
        {
            public static readonly PropertyInfo NavigationProperty =
                typeof(OneToManyPrincipal).GetProperty("ManyToManyFirsts", BindingFlags.Public | BindingFlags.Instance);

            public int Id { get; set; }
            public IEnumerable<ManyToManyFirst> ManyToManyFirsts { get; set; }
        }

        public class MultipleNavigationsFirst
        {
            public static readonly PropertyInfo CollectionNavigationProperty =
                typeof(MultipleNavigationsFirst).GetProperty("MultipleNavigationsSeconds", BindingFlags.Public | BindingFlags.Instance);
            public static readonly PropertyInfo NonCollectionNavigationProperty =
                typeof(MultipleNavigationsFirst).GetProperty("MultipleNavigationsSecond", BindingFlags.Public | BindingFlags.Instance);

            public int Id { get; set; }

            public IEnumerable<MultipleNavigationsSecond> MultipleNavigationsSeconds { get; set; }
            public MultipleNavigationsSecond MultipleNavigationsSecond { get; set; }
        }

        public class MultipleNavigationsSecond
        {
            public static readonly PropertyInfo CollectionNavigationProperty =
                typeof(MultipleNavigationsSecond).GetProperty("MultipleNavigationsFirsts", BindingFlags.Public | BindingFlags.Instance);
            public static readonly PropertyInfo NonCollectionNavigationProperty =
                typeof(MultipleNavigationsSecond).GetProperty("MultipleNavigationsFirst", BindingFlags.Public | BindingFlags.Instance);

            public int Id { get; set; }

            public IEnumerable<MultipleNavigationsFirst> MultipleNavigationsFirsts { get; set; }
            public MultipleNavigationsFirst MultipleNavigationsFirst { get; set; }
            
            public static void IgnoreCollectionNavigation(InternalEntityBuilder entityBuilder)
            {
                if (entityBuilder.Metadata.Type == typeof(MultipleNavigationsSecond))
                {
                    entityBuilder.Ignore(CollectionNavigationProperty.Name, ConfigurationSource.DataAnnotation);
                }
            }

            public static void IgnoreNonCollectionNavigation(InternalEntityBuilder entityBuilder)
            {
                if (entityBuilder.Metadata.Type == typeof(MultipleNavigationsSecond))
                {
                    entityBuilder.Ignore(NonCollectionNavigationProperty.Name, ConfigurationSource.DataAnnotation);
                }
            }
        }
    }
}
