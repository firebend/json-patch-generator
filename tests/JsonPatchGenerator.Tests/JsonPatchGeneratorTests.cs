using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Firebend.JsonPatch.Extensions;
using Firebend.JsonPatch.JsonSerializationSettings;
using FluentAssertions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Firebend.JsonPatch.Tests
{
    [TestClass]
    public class JsonPatchGeneratorTests
    {
        private static IJsonPatchGenerator CreateGenerator(JsonSerializerSettings settings = null)
        {
            settings ??= DefaultJsonSerializationSettings.Configure();

            var settingsProvider = new JsonDiffSettingsProvider(settings);
            var writer = new JsonPatchWriter();
            var detector = new JsonDiffDetector(settingsProvider);
            var generator = new DefaultJsonPatchGenerator(detector, writer, settingsProvider);
            return generator;
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Generate_Patch()
        {
            //arrange
            var a = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                Remove = "get it outta here!",
                BirthDate = new DateTime(1964, 2, 22),
                Cases = new List<Case> { new() { Subject = "Flukeman" }, new() { Subject = "Wayne Weinseider" } },
                KnownAddresses = new List<Address>
                {
                    new() { City = "Here", State = "XX", Street = "Fake 123 Street" }, new() { City = "There", State = "YY", Street = "Tester 123 Blvd" }
                }
            };

            var b = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                BadgeNumber = "2317-616",
                BirthDate = new DateTime(1964, 2, 23),
                Cases = new List<Case>
                {
                    new() { Subject = "Flukeman" },
                    new() { Subject = "Wayne Weinseider", Solved = true, SolvedDate = new DateTime(1999, 1, 3) },
                    new() { Subject = "Eddie Van Blundht Jr" },
                    new() { Subject = "The Peacock Family" }
                },
                KnownAddresses = new List<Address> { new() { City = "There", State = "YY", Street = "Tester 123 Blvd" } }
            };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Should().NotBeNull();
            patch.ValuesShouldNotContainJson();

            var expectedOperations = new List<Operation>
            {
                new() { from = null, op = "remove", path = "/Remove", value = null },
                new() { from = null, op = "add", path = "/BadgeNumber", value = "2317-616" },
                new() { from = null, op = "replace", path = "/BirthDate", value = new DateTime(1964, 2, 23).ToString(CultureInfo.CurrentCulture) },
                new() { from = null, op = "add", path = "/Cases/1/Solved", value = "True" },
                new() { from = null, op = "add", path = "/Cases/1/SolvedDate", value = new DateTime(1999, 1, 3).ToString(CultureInfo.CurrentCulture) },
                new()
                {
                    from = null,
                    op = "add",
                    path = "/Cases/-",
                    value = new Case { Subject = "Eddie Van Blundht Jr", Solved = false, SolvedDate = DateTime.MinValue }
                },
                new()
                {
                    //6
                    from = null,
                    op = "add",
                    path = "/Cases/-",
                    value = new Case { Subject = "The Peacock Family", Solved = false, SolvedDate = DateTime.MinValue }
                },
                new() { from = null, op = "replace", path = "/KnownAddresses/0/Street", value = "Tester 123 Blvd" },
                new() { from = null, op = "replace", path = "/KnownAddresses/0/City", value = "There" },
                new() { from = null, op = "replace", path = "/KnownAddresses/0/State", value = "YY" },
                new() { from = null, op = "remove", path = "/KnownAddresses/1", value = null }
            };

            expectedOperations.Count.Should().Be(patch.Operations.Count);

            foreach (var operation in patch.Operations)
            {
                var expected = expectedOperations.FirstOrDefault(x => x.path.EqualsIgnoreCaseAndWhitespace(operation.path) && x.op.EqualsIgnoreCaseAndWhitespace(operation.op));
                expected.Should().NotBeNull("operation with path {0} and verb {1} should be in patch {2}", operation.path, operation.op, JsonConvert.SerializeObject(expectedOperations));

                if (expected!.value is null)
                {
                    operation.value.Should().BeNull();
                }
                else
                {
                    expected.value.ToString().Should().BeEquivalentTo(operation.value.ToString());
                }
            }

            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Remove_Item()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string> { "1", "2", "3" } };

            var b = new CollectionClass { Values = new List<string> { "1", "2" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"path\":\"/values/2\",\"op\":\"remove\"}]").Should().BeTrue();
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Remove_Item_Object()
        {
            //arrange
            var a = new CollectionClass<Believer> { Values = new List<Believer> { new(), new(true), new(true) } };

            var b = new CollectionClass<Believer> { Values = new List<Believer> { new(), new(true) } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"path\":\"/values/2\",\"op\":\"remove\"}]").Should().BeTrue();
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Remove_Many_Items()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string>
                {
                    "1",
                    "2",
                    "3",
                    "4",
                    "5"
                }
            };

            var b = new CollectionClass { Values = new List<string> { "1" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(4);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace(
                    "[{\"path\":\"/values/4\",\"op\":\"remove\"},{\"path\":\"/values/3\",\"op\":\"remove\"},{\"path\":\"/values/2\",\"op\":\"remove\"},{\"path\":\"/values/1\",\"op\":\"remove\"}]")
                .Should()
                .BeTrue();
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Add_Many_Items()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string> { "1" } };

            var b = new CollectionClass
            {
                Values = new List<string>
                {
                    "1",
                    "2",
                    "3",
                    "4",
                    "5"
                }
            };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(4);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.Should()
                .BeEquivalentTo(
                    "[{\"value\":\"2\",\"path\":\"/values/-\",\"op\":\"add\"},{\"value\":\"3\",\"path\":\"/values/-\",\"op\":\"add\"},{\"value\":\"4\",\"path\":\"/values/-\",\"op\":\"add\"},{\"value\":\"5\",\"path\":\"/values/-\",\"op\":\"add\"}]");
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Add_Item()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string> { "1", "2" } };

            var b = new CollectionClass { Values = new List<string> { "1", "2", "3" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.Should().BeEquivalentTo("[{\"value\":\"3\",\"path\":\"/values/-\",\"op\":\"add\"}]");
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Add_Item_Object()
        {
            //arrange
            var a = new CollectionClass<Believer> { Values = new List<Believer> { new(), new(true) } };

            var b = new CollectionClass<Believer> { Values = new List<Believer> { new(), new(true), new(true) } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.Should().BeEquivalentTo("[{\"value\":{\"WantsToBelieve\":true},\"path\":\"/Values/-\",\"op\":\"add\"}]");
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Null_Array_Replace_With_Array()
        {
            //arrange
            var a = new CollectionClassArray { Values = null };


            var b = new CollectionClassArray
            {
                Values = new[]
                {
                    "1",
                    "2",
                    "3"
                }
            };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.Should().BeEquivalentTo("[{\"value\":[\"1\",\"2\",\"3\"],\"path\":\"/Values\",\"op\":\"add\"}]");
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Null_List_Replace_With_List_Object()
        {
            //arrange
            var a = new CollectionClass<Believer> { Values = null };


            var b = new CollectionClass<Believer> { Values = new List<Believer> { new(), new(true), new(true) } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Empty_List_Replace_With_List()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string>() };

            var b = new CollectionClass { Values = new List<string> { "1", "2", "3" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(3);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            const string shouldBeJson =
                "[{\"value\":\"1\",\"path\":\"/Values/0\",\"op\":\"add\"},{\"value\":\"2\",\"path\":\"/Values/1\",\"op\":\"add\"},{\"value\":\"3\",\"path\":\"/Values/2\",\"op\":\"add\"}]";
            json.Should().BeEquivalentTo(shouldBeJson);
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Empty_List_Replace_With_List_Object()
        {
            //arrange
            var a = new CollectionClass<Believer> { Values = new List<Believer>() };

            var b = new CollectionClass<Believer> { Values = new List<Believer> { new(true), new() } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(2);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            const string shouldBeJson =
                "[{\"value\":{\"WantsToBelieve\":true},\"path\":\"/Values/0\",\"op\":\"add\"},{\"value\":{\"WantsToBelieve\":false},\"path\":\"/Values/1\",\"op\":\"add\"}]";
            json.Should().BeEquivalentTo(shouldBeJson);
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Update_At_Beginning()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string> { "0", "2", "3" } };

            var b = new CollectionClass { Values = new List<string> { "1", "2", "3" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.Should().BeEquivalentTo("[{\"value\":\"1\",\"path\":\"/values/0\",\"op\":\"replace\"}]");
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Update_At_End()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string> { "1", "2", "03" } };

            var b = new CollectionClass { Values = new List<string> { "1", "2", "3" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.Should().BeEquivalentTo("[{\"value\":\"3\",\"path\":\"/values/2\",\"op\":\"replace\"}]");
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Update_At_Middle()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string> { "1", "02", "3" } };

            var b = new CollectionClass { Values = new List<string> { "1", "2", "3" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            json.Should().BeEquivalentTo("[{\"value\":\"2\",\"path\":\"/values/1\",\"op\":\"replace\"}]");
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Sorting()
        {
            //arrange
            var a = new CollectionClass { Values = new List<string> { "grape", "apple", "orange" } };

            var b = new CollectionClass { Values = new List<string> { "apple", "grape", "orange" } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(2);
            patch.ValuesShouldNotContainJson();

            var json = JsonConvert.SerializeObject(patch.Operations);
            const string expectedJson =
                "[{\"value\":\"apple\",\"path\":\"/values/0\",\"op\":\"replace\"},{\"value\":\"grape\",\"path\":\"/values/1\",\"op\":\"replace\"}]";
            expectedJson.Should().BeEquivalentTo(json);
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Replace_Null_With_Object()
        {
            //arrange
            var a = new Agent { FirstName = "Dana", LastName = "Scully", Email = "dscully@fbi.gov", Believer = null };

            var b = new Agent { FirstName = "Dana", LastName = "Scully", Email = "dscully@fbi.gov", Believer = new Believer { WantsToBelieve = true } };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Should().NotBeNull();
            patch.ValuesShouldNotContainJson();

            var patchJson = JsonConvert.SerializeObject(patch);
            const string expectedJson = "[{\"value\":{\"WantsToBelieve\":true},\"path\":\"/Believer\",\"op\":\"add\"}]";
            patchJson.Should().BeEquivalentTo(expectedJson);

            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Generate_Empty_Patch()
        {
            //arrange
            var a = new Agent { FirstName = "Dana", LastName = "Scully", Email = "dscully@fbi.gov", Believer = null };

            var b = new Agent { FirstName = "Dana", LastName = "Scully", Email = "dscully@fbi.gov", Believer = null };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Should().NotBeNull();
            patch.ValuesShouldNotContainJson();
            patch.Operations.Should().BeEmpty();
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Empty_List_Of_Object_To_Populated_List()
        {
            //arrange
            var a = new CollectionClass<Agent> { Values = new List<Agent>() };

            var b = new CollectionClass<Agent>
            {
                Values = new List<Agent> { new() { FirstName = "Dana", LastName = "Scully", Email = "dscully@fbi.gov", Believer = null } }
            };

            //act
            var patch = CreateGenerator().Generate(a, b);

            //assert
            patch.Should().NotBeNull();
            patch.ValuesShouldNotContainJson();
            patch.Operations.Should().NotBeEmpty();
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Empty_List_Of_Object_To_Populated_List_With_Custom_Settings()
        {
            //arrange
            var serializerSettings = CreateCustomSettings();

            var a = new CollectionClass<Agent> { Values = new List<Agent>() };

            var b = new CollectionClass<Agent>
            {
                Values = new List<Agent> { new() { FirstName = "Dana", LastName = "Scully", Email = "dscully@fbi.gov", Believer = null } }
            };

            //act
            var patch = CreateGenerator(serializerSettings).Generate(a, b);

            //assert
            patch.Should().NotBeNull();
            patch.ValuesShouldNotContainJson();
            patch.Operations.Should().NotBeEmpty();
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Collection_Complex_Object_Change_With_Custom_Settings()
        {
            //arrange
            var serializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, TypeNameHandling = TypeNameHandling.Objects
            };

            var a = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                Cases = new List<Case> { new() { Subject = "Flukeman", AssignedAgent = new() { FirstName = "Dana", LastName = "Scully" } } }
            };

            var b = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                Cases = new List<Case>
                {
                    new()
                    {
                        Subject = "Flukeman",
                        AssignedAgent = new() { FirstName = "Dana", LastName = "Scully", Email = "d@gov.com", Believer = new(true) }
                    }
                }
            };

            //act
            var patch = CreateGenerator(serializerSettings).Generate(a, b);

            //assert
            patch.Should().NotBeNull();
            patch.ValuesShouldNotContainJson();
            patch.Operations.Should().NotBeEmpty();
            patch.ApplyTo(a);
            a.Should().BeEquivalentTo(b);
        }

        private static JsonSerializerSettings CreateCustomSettings()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            serializerSettings.Converters.Add(new StringEnumConverter());
            serializerSettings.ContractResolver = new CamelCaseExceptDictionaryKeysResolver();

            serializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
            serializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            serializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;

            serializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            serializerSettings.TypeNameHandling = TypeNameHandling.Objects;
            return serializerSettings;
        }

        private class Agent
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Remove { get; set; }
            public string BadgeNumber { get; set; }
            public DateTime BirthDate { get; set; }
            public List<Case> Cases { get; set; }
            public List<Address> KnownAddresses { get; set; }
            public Believer Believer { get; set; }
        }

        private class Believer
        {
            public Believer() { }

            public Believer(bool wantsToBelieve)
            {
                WantsToBelieve = wantsToBelieve;
            }

            public bool WantsToBelieve { get; set; }
        }

        private class Case
        {
            public string Subject { get; set; }

            public bool Solved { get; set; }

            public DateTime SolvedDate { get; set; }

            public Agent AssignedAgent { get; set; }
        }

        private class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string State { get; set; }
        }

        private class CollectionClass
        {
            public List<string> Values { get; set; }
        }

        private class CollectionClass<T>
        {
            public List<T> Values { get; set; }
        }

        private class CollectionClassArray
        {
            public string[] Values { get; set; }
        }

        private class CamelCaseExceptDictionaryKeysResolver : CamelCasePropertyNamesContractResolver
        {
            protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
            {
                var contract = base.CreateDictionaryContract(objectType);

                contract.DictionaryKeyResolver = propertyName => propertyName;

                return contract;
            }
        }
    }
}
