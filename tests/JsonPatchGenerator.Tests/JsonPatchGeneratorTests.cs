using System;
using System.Collections.Generic;
using System.Globalization;
using Firebend.JsonPatch.Extensions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Firebend.JsonPatch.Tests
{
    [TestClass]
    public class JsonPatchGeneratorTests
    {
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
            public bool WantsToBelieve { get; set; }

            public Believer() { }

            public Believer(bool wantsToBelieve)
            {
                WantsToBelieve = wantsToBelieve;
            }
        }

        private class Case
        {
            public string Subject { get; set; }

            public bool Solved { get; set; }

            public DateTime SolvedDate { get; set; }
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
                Cases = new List<Case>
                {
                    new()
                    {
                        Subject = "Flukeman"
                    },
                    new()
                    {
                        Subject = "Wayne Weinseider"
                    }
                },
                KnownAddresses = new List<Address>
                {
                    new()
                    {
                        City = "Here",
                        State = "XX",
                        Street = "Fake 123 Street"

                    },
                    new()
                    {
                        City = "There",
                        State = "YY",
                        Street = "Tester 123 Blvd"
                    }
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
                    new()
                    {
                        Subject = "Flukeman",
                    },
                    new()
                    {
                        Subject = "Wayne Weinseider",
                        Solved = true,
                        SolvedDate = new DateTime(1999, 1, 3)
                    },
                    new()
                    {
                        Subject = "Eddie Van Blundht Jr"
                    },
                    new()
                    {
                        Subject = "The Peacock Family"
                    }
                },
                KnownAddresses = new List<Address>
                {
                    new()
                    {
                        City = "There",
                        State = "YY",
                        Street = "Tester 123 Blvd"
                    }
                }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Should().NotBeNull();

            var patchDeserializedOperations = patch.Operations;

            var expectedOperations = new List<Microsoft.AspNetCore.JsonPatch.Operations.Operation>
            {
                new()
                {
                    from = null,
                    op = "remove",
                    path = "/Remove",
                    value = null
                },
                new()
                {
                    from = null,
                    op = "add",
                    path = "/BadgeNumber",
                    value = "2317-616"
                },
                new()
                {
                    from = null,
                    op = "replace",
                    path = "/BirthDate",
                    value = new DateTime(1964, 2, 23).ToString(CultureInfo.CurrentCulture)
                },
                new()
                {
                    from = null,
                    op = "add",
                    path = "/Cases/1/Solved",
                    value = "True"
                },
                new()
                {
                    from = null,
                    op = "add",
                    path = "/Cases/1/SolvedDate",
                    value = new DateTime(1999, 1, 3).ToString(CultureInfo.CurrentCulture)
                },
                new()
                {
                    from = null,
                    op = "add",
                    path = "/Cases/-",
                    value = new Case
                    {
                        Subject = "Eddie Van Blundht Jr",
                        Solved = false,
                        SolvedDate = DateTime.MinValue
                    }
                },
                new()
                {
                    //6
                    from = null,
                    op = "add",
                    path = "/Cases/-",
                    value = new Case
                    {
                        Subject = "The Peacock Family",
                        Solved = false,
                        SolvedDate = DateTime.MinValue
                    }
                },
                new()
                {
                    from = null,
                    op = "replace",
                    path = "/KnownAddresses/0/Street",
                    value = "Tester 123 Blvd"
                },
                new()
                {
                    from = null,
                    op = "replace",
                    path = "/KnownAddresses/0/City",
                    value = "There"
                },
                new()
                {
                    from = null,
                    op = "replace",
                    path = "/KnownAddresses/0/State",
                    value = "YY"
                },
                new()
                {
                    from = null,
                    op = "remove",
                    path = "/KnownAddresses/1",
                    value = null
                }
            };

            patchDeserializedOperations.Should().BeEquivalentTo(expectedOperations);

            var aClone = a.Clone();
            patch.ApplyTo(aClone);
            aClone.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Remove_Item()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "1", "2", "3" }
            };

            var b = new CollectionClass
            {
                Values = new List<string> { "1", "2" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"path\":\"/values/2\",\"op\":\"remove\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Remove_Item_Object()
        {
            //arrange
            var a = new CollectionClass<Believer>
            {
                Values = new List<Believer> { new(), new(true), new(true) }
            };

            var b = new CollectionClass<Believer>
            {
                Values = new List<Believer> { new(), new(true) }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"path\":\"/values/2\",\"op\":\"remove\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Remove_Many_Items()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "1", "2", "3", "4", "5" }
            };

            var b = new CollectionClass
            {
                Values = new List<string> { "1" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(4);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"path\":\"/values/4\",\"op\":\"remove\"},{\"path\":\"/values/3\",\"op\":\"remove\"},{\"path\":\"/values/2\",\"op\":\"remove\"},{\"path\":\"/values/1\",\"op\":\"remove\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Add_Many_Items()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "1" }
            };

            var b = new CollectionClass
            {
                Values = new List<string> { "1", "2", "3", "4", "5" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(4);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":\"2\",\"path\":\"/values/-\",\"op\":\"add\"},{\"value\":\"3\",\"path\":\"/values/-\",\"op\":\"add\"},{\"value\":\"4\",\"path\":\"/values/-\",\"op\":\"add\"},{\"value\":\"5\",\"path\":\"/values/-\",\"op\":\"add\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Add_Item()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "1", "2" }
            };

            var b = new CollectionClass
            {

                Values = new List<string> { "1", "2", "3" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":\"3\",\"path\":\"/values/-\",\"op\":\"add\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Add_Item_Object()
        {
            //arrange
            var a = new CollectionClass<Believer>
            {
                Values = new List<Believer> { new(), new(true) }
            };

            var b = new CollectionClass<Believer>
            {
                Values = new List<Believer> { new(), new(true), new (true)}
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":{\"WantsToBelieve\":true},\"path\":\"/Values/-\",\"op\":\"add\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Null_Array_Replace_With_Array()
        {
            //arrange
            var a = new CollectionClassArray {Values = null}; //new CollectionClass


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
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":[\"1\",\"2\",\"3\"],\"path\":\"/Values\",\"op\":\"add\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Null_List_Replace_With_List_Object()
        {
            //arrange
            var a = new CollectionClass<Believer> {Values = null}; //new CollectionClass


            var b = new CollectionClass<Believer>
            {
                Values = new List<Believer>
                {
                    new(),
                    new (true),
                    new(true)
                }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            const string shouldBeJson =
                "[{\"value\":[{\"WantsToBelieve\":false},{\"WantsToBelieve\":true},{\"WantsToBelieve\":true}],\"path\":\"/Values\",\"op\":\"add\"}]";
            json.EqualsIgnoreCaseAndWhitespace(shouldBeJson).Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Empty_List_Replace_With_List()
        {
            //arrange
            var a = new CollectionClass {Values = new List<string>() }; //new CollectionClass

            var b = new CollectionClass
            {
                Values = new List<string>
                {
                    "1",
                    "2",
                    "3"
                }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(3);
            var json = JsonConvert.SerializeObject(patch.Operations);
            const string shouldBeJson = "[{\"value\":\"1\",\"path\":\"/Values/0\",\"op\":\"add\"},{\"value\":\"2\",\"path\":\"/Values/1\",\"op\":\"add\"},{\"value\":\"3\",\"path\":\"/Values/2\",\"op\":\"add\"}]";
            json.EqualsIgnoreCaseAndWhitespace(shouldBeJson).Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Empty_List_Replace_With_List_Object()
        {
            //arrange
            var a = new CollectionClass<Believer> {Values = new List<Believer>() }; //new CollectionClass

            var b = new CollectionClass<Believer>
            {
                Values = new List<Believer>
                {
                    new(true),
                    new()
                }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(2);
            var json = JsonConvert.SerializeObject(patch.Operations);
            const string shouldBeJson = "[{\"value\":{\"WantsToBelieve\":true},\"path\":\"/Values/0\",\"op\":\"add\"},{\"value\":{\"WantsToBelieve\":false},\"path\":\"/Values/1\",\"op\":\"add\"}]";
            json.EqualsIgnoreCaseAndWhitespace(shouldBeJson).Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Update_At_Beginning()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "0", "2", "3" }
            };

            var b = new CollectionClass
            {
                Values = new List<string> { "1", "2", "3" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":\"1\",\"path\":\"/values/0\",\"op\":\"replace\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Update_At_End()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "1", "2", "03" }
            };

            var b = new CollectionClass
            {
                Values = new List<string> { "1", "2", "3" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":\"3\",\"path\":\"/values/2\",\"op\":\"replace\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Array_Update_At_Middle()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "1", "02", "3" }
            };

            var b = new CollectionClass
            {
                Values = new List<string> { "1", "2", "3" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(1);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":\"2\",\"path\":\"/values/1\",\"op\":\"replace\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Handle_Sorting()
        {
            //arrange
            var a = new CollectionClass
            {
                Values = new List<string> { "grape", "apple", "orange" }
            };

            var b = new CollectionClass
            {
                Values = new List<string> { "apple", "grape", "orange" }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Operations.Should().HaveCount(2);
            var json = JsonConvert.SerializeObject(patch.Operations);
            json.EqualsIgnoreCaseAndWhitespace("[{\"value\":\"apple\",\"path\":\"/values/0\",\"op\":\"replace\"},{\"value\":\"grape\",\"path\":\"/values/1\",\"op\":\"replace\"}]").Should().BeTrue();
            var test = a.Clone();
            patch.ApplyTo(test);
            test.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Replace_Null_With_Object()
        {
            //arrange
            var a = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                Believer = null
            };

            var b = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                Believer = new Believer
                {
                    WantsToBelieve = true
                }
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Should().NotBeNull();

            var patchJson = JsonConvert.SerializeObject(patch);
            const string expectedJson = "[{\"value\":{\"WantsToBelieve\":true},\"path\":\"/Believer\",\"op\":\"add\"}]";
            patchJson.EqualsIgnoreCaseAndWhitespace(expectedJson).Should().BeTrue();

            var aClone = a.Clone();
            patch.ApplyTo(aClone);
            aClone.Should().BeEquivalentTo(b);
        }

        [TestMethod]
        public void Json_Patch_Document_Generator_Should_Generate_Empty_Patch()
        {
            //arrange
            var a = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                Believer = null
            };

            var b = new Agent
            {
                FirstName = "Dana",
                LastName = "Scully",
                Email = "dscully@fbi.gov",
                Believer = null
            };

            //act
            var patch = new JsonPatchGenerator().Generate(a, b);

            //assert
            patch.Should().NotBeNull();
            patch.Operations.Should().BeEmpty();
        }
    }
}
