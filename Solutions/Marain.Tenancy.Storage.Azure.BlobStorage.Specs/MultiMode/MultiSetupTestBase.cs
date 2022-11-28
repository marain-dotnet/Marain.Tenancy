// <copyright file="MultiSetupTestBase.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using NUnit.Framework.Interfaces;
    using NUnit.Framework.Internal;
    using NUnit.Framework.Internal.Builders;

    /// <summary>
    /// Base class for tests that need to perform setup both by using the subject under test (to
    /// prove that it can be done) and also by writing data directly into blob storage (to prove
    /// that the code works when the data in storage was created with a previous version of the
    /// library).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tests can derive from this and must set the following class-level attribute:
    /// </para>
    /// <code><![CDATA[
    /// [MultiSetupTest]
    /// ]]></code>
    /// <para>
    /// This needs to be specified on each deriving class, because NUnit does not walk up the
    /// inheritance chain when looking for test fixture sources.
    /// </para>
    /// <para>
    /// Deriving classes should also define a constructor that has the same signature as this
    /// class's constructor, forwarding the argument on. This, in conjunction with the test
    /// fixture source attribute, will cause NUnit to run the fixture for this class multiple
    /// times, once for each of the host types specified in <see cref="FixtureArgs"/>.
    /// </para>
    /// <para>
    /// When using SpecFlow, bindings can detect the mode by casting the reference in
    /// <c>TestExecutionContext.CurrentContext.TestObject</c> to <see cref="IMultiModeTest{TestHostTypes}"/>
    /// and then inspecting the <see cref="IMultiModeTest{TestHostTypes>.TestType"/> property.
    /// </para>
    /// </remarks>
    public class MultiSetupTestBase : IMultiModeTest<SetupModes>
    {
        private static readonly object[] FixtureArgs =
        {
            new object[] { SetupModes.ViaApiPropagateRootConfigAsV2 },
            new object[] { SetupModes.ViaApiPropagateRootConfigAsV3 },
            new object[] { SetupModes.DirectToStoragePropagateRootConfigAsV2 },
            new object[] { SetupModes.DirectToStoragePropagateRootConfigAsV3 },
        };

        /// <summary>
        /// Creates a <see cref="MultiSetupTestBase"/>.
        /// </summary>
        /// <param name="testType">
        /// Setup style to test with.
        /// </param>
        private protected MultiSetupTestBase(SetupModes testType)
        {
            this.TestType = testType;
        }

        /// <inheritdoc/>
        public SetupModes TestType { get; }

        /// <summary>
        /// Attribute to be applied to tests deriving from <see cref="MultiSetupTestBase"/> to
        /// create multiple instances of the test, one for each test mode.
        /// </summary>
        /// <remarks>
        /// Annoyingly, applying this to the base type doesn't work. NUnit appears not to support
        /// inheritance of fixture-building test attributes, which is why we can't just slap this
        /// on <see cref="MultiSetupTestBase"/> once and for all.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Class)]
        public class MultiSetupTestAttribute : Attribute, IFixtureBuilder2
        {
            private readonly NUnitTestFixtureBuilder builder = new();

            /// <inheritdoc/>
            public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo, IPreFilter filter)
            {
                // This whole method does more or less the same thing as NUnit's own
                // TestFixtureSourceAttribute, but with one difference: it is hard-coded to use
                // the list of test modes as its input.
                var fixtureSuite = new ParameterizedFixtureSuite(typeInfo);
                fixtureSuite.ApplyAttributesToTest(typeInfo.Type.GetTypeInfo());
                ICustomAttributeProvider assemblyLifeCycleAttributeProvider = typeInfo.Type.GetTypeInfo().Assembly;
                ICustomAttributeProvider typeLifeCycleAttributeProvider = typeInfo.Type.GetTypeInfo();

                foreach (object[] args in FixtureArgs.Cast<object[]>())
                {
                    var arg = (SetupModes)args[0];
                    ITestFixtureData parms = new TestFixtureParameters(new object[] { arg });
                    TestSuite fixture = this.builder.BuildFrom(typeInfo, filter, parms);

                    fixture.ApplyAttributesToTest(assemblyLifeCycleAttributeProvider);
                    fixture.ApplyAttributesToTest(typeLifeCycleAttributeProvider);
                    fixtureSuite.Add(fixture);
                }

                yield return fixtureSuite;
            }

            /// <inheritdoc/>
            public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo)
            {
                return this.BuildFrom(typeInfo, NullPrefilter.Instance);
            }

            private class NullPrefilter : IPreFilter
            {
                public static readonly NullPrefilter Instance = new();

                public bool IsMatch(Type type) => true;

                public bool IsMatch(Type type, MethodInfo method) => true;
            }
        }
    }
}