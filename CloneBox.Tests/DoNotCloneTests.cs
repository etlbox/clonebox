using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CloneBox.Tests {
    public class DoNotCloneTests {

        public class CheckIgnoreAttribute {
            public string Prop1 { get; set; }
            [DoNotClone]
            public string Prop2 { get; set; }
            public string Prop3 { get; set; }
            [DoNotClone]
            public string Prop4 { get; set; }
            public string Prop5 { get; set; }
            public string Field1;
            [DoNotClone]
            public string Field2;
        }

        [Fact]
        public void AttributeOnPropertiesAndFields() {

            CheckIgnoreAttribute orig = new CheckIgnoreAttribute() {
                Prop1 = "1",
                Prop2 = "2",
                Prop3 = "3",
                Prop4 = "4",
                Prop5 = "5",
                Field1 = "6",
                Field2 = "7"

            };

            var clone = orig.CloneX();  

            clone.Prop1.Should().Be("1");
            clone.Prop2.Should().BeNull();
            clone.Prop3.Should().Be("3");
            clone.Prop4.Should().BeNull();
            clone.Prop5.Should().Be("5");
            clone.Field1.Should().Be("6");
            clone.Field2.Should().BeNull();
        }

        public class DoNotCloneContainer {
            public DoNotCloneClass Prop { get; set; }
            public DoNotCloneClass Field;
            public List<DoNotCloneClass> PropList { get; set; }
        }

        [DoNotClone]
        public class DoNotCloneClass {
            public string Name { get; set; }
        }

        [Fact]
        public void AttributeOnClass() {

            DoNotCloneContainer orig = new DoNotCloneContainer() {
                Prop = new DoNotCloneClass() { Name = "A" },
                Field = new DoNotCloneClass() { Name = "B" },
                PropList = new List<DoNotCloneClass>() {
                    new DoNotCloneClass() { Name = "C" },
                    new DoNotCloneClass() { Name = "D" }
                }
            };

            var clone = orig.CloneX();

            clone.Prop.Should().NotBeNull();
            clone.Prop.Name.Should().BeNull();
            clone.Field.Should().BeNull();
            clone.PropList.Should().HaveCount(2);
            clone.PropList.ElementAt(0).Should().BeNull();
            clone.PropList.ElementAt(1).Should().BeNull();
        }


        public class CheckIgnorePredicates {
            public string Prop1 { get; set; }
            public string Prop2 { get; set; }
            public string Prop3 { get; set; }
            public string Prop4 { get; set; }
            public string Prop5 { get; set; }
            public string Field1;
            public string Field2;

            public InnerClass Inner1 { get; set; }
            public InnerClass Inner2 { get; set; }
            public class InnerClass {
                public string X { get; set; }
            }
        }

        [Fact]
        public void UsingPredicates() {

            CheckIgnorePredicates orig = new CheckIgnorePredicates() {
                Prop1 = "1",
                Prop2 = "2",
                Prop3 = "3",
                Prop4 = "4",
                Prop5 = "5",
                Field1 = "6",
                Field2 = "7",
                Inner1 = new CheckIgnorePredicates.InnerClass() { X = "8" },
                Inner2 = new CheckIgnorePredicates.InnerClass() { X = "9" },

            };

            int count = 1;
            var clone = orig.CloneX(new CloneSettings() {
                DoNotCloneProperty = (prop) => prop.Name == "Prop2" || prop.Name == "Prop4",
                DoNotCloneField = (field) => field.Name == "Field2",
                DoNotCloneClass = (type) => type == typeof(CheckIgnorePredicates.InnerClass) && count++ == 1
            });
            clone.Prop1.Should().Be("1");
            clone.Prop2.Should().BeNull();
            clone.Prop3.Should().Be("3");
            clone.Prop4.Should().BeNull();
            clone.Prop5.Should().Be("5");
            clone.Field1.Should().Be("6");
            clone.Field2.Should().BeNull();
            clone.Inner1.X.Should().BeNull();
            clone.Inner2.X.Should().Be("9");
            
        }
    }
}
