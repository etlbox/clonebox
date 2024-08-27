using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class ComplexClassTests {

        #region Complex Object 1
        public class ComplexObject : IEquatable<ComplexObject>, IDisposable {
            private bool _isDisposed;
            private readonly int _constField;

            public delegate void TestDelegate(int value);
            public TestDelegate MyTestDelegate { get; set; }
#pragma warning disable 0067
            public event TestDelegate OnTestDelegate;
#pragma warning restore 0067

            public List<string> ListOfStrings = new List<string>();
            public ATestClass TestClassNoSetter { get; }
            private readonly ATestClass _anotherTestClass;

            public ComplexObject(int initialValue) {
                _isDisposed = false;
                _constField = initialValue;
                MyTestDelegate = MyTestMethod;

                TestClassNoSetter = new ATestClass() {
                    Name = "Read-only Property test",
                    Description = "A class assigned to a public read-only property",
                    TestInterface = new InterfaceObject { BoolValue = false, IntValue = 456 }
                };

                _anotherTestClass = new ATestClass() {
                    Name = "Private field test",
                    Description = "A class assigned to a private field",
                    TestInterface = new InterfaceObject { BoolValue = true, IntValue = 123 }
                };
            }

            private void MyTestMethod(int value) {
                // this doesn't do anything
            }

            public override int GetHashCode() => base.GetHashCode();

            public override bool Equals(object obj) {
                var basicObject = (ComplexObject)obj;
                return Equals(basicObject);
            }

            public bool Equals(ComplexObject other) {
                var e1 = _isDisposed == other?._isDisposed;
                var e2 = _constField == other?._constField;
                var e3 = ListOfStrings.AsEnumerable().SequenceEqual(other?.ListOfStrings);
                var e4 = TestClassNoSetter.Equals(other?.TestClassNoSetter);
                var e5 = _anotherTestClass.Equals(other?._anotherTestClass);

                return e1 && e2 && e3 && e4 && e5;
            }

            protected virtual void Dispose(bool isDisposing) => _isDisposed = true;

            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public class InterfaceObject : ITestInterface, IEquatable<InterfaceObject> {
            public bool BoolValue { get; set; }
            public int IntValue { get; set; }
            public IDictionary<int, BasicObject> DictionaryValue { get; set; } = new Dictionary<int, BasicObject>();

            public override int GetHashCode() {
                return base.GetHashCode();
            }
            public override bool Equals(object obj) {
                var basicObject = (InterfaceObject)obj;
                return Equals(basicObject);
            }

            public bool Equals(InterfaceObject other) {
                var collectionComparer = new DictionaryComparer<int, BasicObject>();
                var dictionaryIsEqual = collectionComparer.Equals(DictionaryValue, other.DictionaryValue);

                return dictionaryIsEqual && BoolValue == other.BoolValue && IntValue == other.IntValue;
            }

            public class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>> {
                private IEqualityComparer<TValue> valueComparer;
                public DictionaryComparer(IEqualityComparer<TValue> valueComparer = null) {
                    this.valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
                }

                public bool Equals(IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y) {
                    if (x.Count != y.Count)
                        return false;
                    if (x.Keys.Except(y.Keys).Any())
                        return false;
                    if (y.Keys.Except(x.Keys).Any())
                        return false;
                    foreach (var pair in x)
                        if (!valueComparer.Equals(pair.Value, y[pair.Key]))
                            return false;
                    return true;
                }

                public int GetHashCode(IDictionary<TKey, TValue> obj) {
                    return 0;
                }
            }
        }

        public class ATestClass : IEquatable<ATestClass> {
            public string Name { get; set; }
            public string Description { get; set; }
            public ITestInterface TestInterface { get; set; }

            public override int GetHashCode() => base.GetHashCode();

            public override bool Equals(object obj) {
                var basicObject = (ATestClass)obj;
                return Equals(basicObject);
            }

            public bool Equals(ATestClass other) {
                if (other is null)
                    return false;
                return Name?.Equals(other.Name, StringComparison.CurrentCulture) == true
                    && Description?.Equals(other.Description, StringComparison.CurrentCulture) == true
                    && TestInterface?.Equals(other.TestInterface) == true;
            }
        }

        public interface ITestInterface {
            bool BoolValue { get; set; }
            int IntValue { get; set; }
            IDictionary<int, BasicObject> DictionaryValue { get; set; }
        }

        public class BasicObject : IEquatable<BasicObject> {
            private int _privateIntValue;
            private int _randomIdentifier;
            public bool BoolValue { get; set; }
            public byte ByteValue { get; set; }
            public int IntValue { get; set; }
            public long LongValue { get; set; }
            public string StringValue { get; set; }

            public BasicObject() {
                _randomIdentifier = new Random((int)DateTime.UtcNow.Ticks).Next(1, 999999);
            }

            public BasicObject(int privateIntValue) : this() {
                _privateIntValue = privateIntValue;
            }

            public override int GetHashCode() => base.GetHashCode();

            public override bool Equals(object obj) {
                if (obj == null || obj.GetType() != typeof(BasicObject))
                    return false;

                var basicObject = (BasicObject)obj;
                return Equals(basicObject);
            }

            public bool Equals(BasicObject other) {
                if (other == null)
                    return false;
                return
                    other._privateIntValue == _privateIntValue
                    && other._randomIdentifier == _randomIdentifier
                    && other.BoolValue == BoolValue
                    && other.ByteValue == ByteValue
                    && other.IntValue == IntValue
                    && other.LongValue == LongValue
                    && other.StringValue == StringValue;
            }
        }

        [Fact]
        public void CloneComplexClass() {
            var orig = new ComplexObject(100);
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Equals(orig).Should().BeTrue();
            
        }

        #endregion

        #region Complex Invoice Object 

        [Serializable]
        public class InvoiceSource {
            public string Identifier { get; set; }
            public InvoiceGroup[] InvoiceGroups { get; set; }
            public InvoiceOutlet[] Outlets { get; set; }
            public InvoiceSourceMediaTypes MediaType { get; set; }

            public DateTime StartDate =>
                InvoiceGroups?.Select(x => x.StartDate).Min() ?? default(DateTime);
            public DateTime EndDate =>
                InvoiceGroups?.Select(x => x.EndDate).Max() ?? default(DateTime);

            private DateTime _originalProposalStartDate;
            public DateTime OriginalProposalStartDate {
                get => _originalProposalStartDate;
                set {
                    _originalProposalStartDate = value;
                    AdjustedProposalStartDate = new DateTime(2021, 1, 1);
                }
            }

            private DateTime _originalProposalEndDate;
            public DateTime OriginalProposalEndDate {
                get => _originalProposalEndDate;
                set {
                    _originalProposalEndDate = value;
                    AdjustedProposalEndDate = new DateTime(2021, 3, 1);
                }
            }

            public DateTime AdjustedProposalStartDate { get; private set; }
            public DateTime AdjustedProposalEndDate { get; private set; }
        }

        [Serializable]
        public class InvoiceOutlet {
            public string Identifier { get; set; }
            public string Name { get; set; }
            public InvoiceSourceMediaTypes MediaType { get; set; }
            public short? MarketZone { get; set; }
        }

        [Serializable]
        public class InvoiceGroup {
            public string Identifier { get; set; }
            public Invoice[] Invoices { get; set; }
            public InvoiceDemoCategory[] DemoCategories { get; set; }
            public bool IsPackage { get; set; }

            public DateTime StartDate =>
                Invoices?.Select(x => x.StartDate).Min() ?? default(DateTime);
            public DateTime EndDate =>
                Invoices?.Select(x => x.EndDate).Max() ?? default(DateTime);
        }

        [Serializable]
        public class InvoiceDemoCategory {
            public string Identifier { get; set; }
            public short AgeFrom { get; set; }
            public short AgeTo { get; set; }
            public string Group { get; set; }
            public DemographicValueTypes DemoValueType { get; set; }
        }

        [Serializable]
        public class Invoice {
            public string UUID { get; set; }
            public string Name { get; set; }
            public string[] ProgramNames { get; set; }
            public InvoicePeriod[] Periods { get; set; }
            public InvoiceDayTime[] DayTimes { get; set; }
            public string SpotLength { get; set; }
            public string OutletName { get; set; }
            public SourceModelType SourceModelType { get; set; } = SourceModelType.SpotCableProposal;
            public object SourceModel { get; set; }
            public int SourceModelLineNumber { get; set; }

            public DateTime StartDate =>
                Periods?.Select(x => x.AdjustedStartDate).Min() ?? default(DateTime);
            public DateTime EndDate =>
                Periods?.Select(x => x.AdjustedEndDate).Max() ?? default(DateTime);
        }

        [Serializable]
        public class InvoicePeriod {
            public DateTime AdjustedStartDate { get; set; }
            public DateTime OriginalStartDate { get; set; }
            public DateTime AdjustedEndDate { get; set; }
            public DateTime OriginalEndDate { get; set; }
            public decimal Rate { get; set; }
            public DemoValue[] DemoValues { get; set; }
            public string SpotsPerWeek { get; set; }
            public ItemsPerDayOfWeek OriginalSpotsPerDayOfWeek { get; set; }
            public DayOfWeek AllocatedDayOfWeek { get; set; }
            public InvoicePeriodType InvoicePeriodType { get; set; } = InvoicePeriodType.DetailedPeriodType;
        }

        [Serializable]
        public class InvoiceDayTime {
            public TimeSpanRange BroadcastTimeRange { get; set; }
            public CustomDaysOfWeek Days { get; set; }
            public string ProgramName { get; set; }

            public override bool Equals(object obj) =>
                obj is InvoiceDayTime time &&
                EqualityComparer<TimeSpanRange>.Default.Equals(BroadcastTimeRange, time.BroadcastTimeRange) &&
                Days == time.Days &&
                ProgramName == time.ProgramName;

            public override int GetHashCode() {
                var hashCode = 1869100972;
                hashCode = hashCode * -1521134295 + EqualityComparer<TimeSpanRange>.Default.GetHashCode(BroadcastTimeRange);
                hashCode = hashCode * -1521134295 + Days.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProgramName);
                return hashCode;
            }
        }

        [Serializable]
        public class TimeSpanRange {
            public TimeSpanRange(TimeSpan startTime, TimeSpan endTime) {
                StartTime = startTime;
                EndTime = endTime;
            }

            public TimeSpan StartTime { get; private set; }
            public TimeSpan EndTime { get; private set; }

            public override bool Equals(object obj) =>
                obj is TimeSpanRange timeSpan &&
                StartTime.Equals(timeSpan.StartTime) &&
                EndTime.Equals(timeSpan.EndTime);

            public override int GetHashCode() {
                var hashCode = -445957783;
                hashCode = hashCode * -1521134295 + StartTime.GetHashCode();
                hashCode = hashCode * -1521134295 + EndTime.GetHashCode();
                return hashCode;
            }
        }

        [Serializable]
        public class DemoValue {
            public string DemoCategoryRef { get; set; }
            public string Value { get; set; }
            public DemographicValueTypes DemoValueType { get; set; }

            public override bool Equals(object obj) {
                var x = this;
                var y = obj as DemoValue;
                return object.ReferenceEquals(x, y) ||
                       (x != null && y != null &&
                        x.DemoCategoryRef.Equals(y.DemoCategoryRef, StringComparison.InvariantCultureIgnoreCase) &&
                        x.DemoValueType == y.DemoValueType &&
                        x.Value.Equals(y.Value, StringComparison.InvariantCultureIgnoreCase));
            }

            public override int GetHashCode() =>
                (DemoCategoryRef ?? string.Empty).GetHashCode() ^
                DemoValueType.GetHashCode() ^
                (Value ?? string.Empty).GetHashCode();
        }

        [Serializable]
        public class ItemsPerDayOfWeek {
            public int Monday { get; set; }
            public int Tuesday { get; set; }
            public int Wednesday { get; set; }
            public int Thursday { get; set; }
            public int Friday { get; set; }
            public int Saturday { get; set; }
            public int Sunday { get; set; }

            public int GetAllocatedItemsForDayOfWeek(DayOfWeek dayOfWeek) {
                switch (dayOfWeek) {
                    case DayOfWeek.Monday:
                        return Monday;
                    case DayOfWeek.Tuesday:
                        return Tuesday;
                    case DayOfWeek.Wednesday:
                        return Wednesday;
                    case DayOfWeek.Thursday:
                        return Thursday;
                    case DayOfWeek.Friday:
                        return Friday;
                    case DayOfWeek.Saturday:
                        return Saturday;
                    case DayOfWeek.Sunday:
                        return Sunday;
                    default:
                        throw new ArgumentException("The value of dayOfWeek must be a valid day of week.");
                }
            }
        }

        [Flags]
        public enum CustomDaysOfWeek {
            None = 0x0000,
            Monday = 0x0001,
            Tuesday = 0x0002,
            Wednesday = 0x0004,
            Thursday = 0x0008,
            Friday = 0x0010,
            Saturday = 0x0020,
            Sunday = 0x0040,
            MondayToFriday = Monday | Tuesday | Wednesday | Thursday | Friday,
            Weekend = Saturday | Sunday,
            All = MondayToFriday | Weekend,
        }

        public enum SourceModelType {
            SpotCableProposal = 1,
            SpotCableOrder = 2
        }

        public enum DemographicValueTypes {
            None = 0,
            Ratings = 1,
            Impressions = 2,
            CPP = 3,
            CPM = 4,
        }

        public enum InvoicePeriodType {
            DetailedPeriodType = 1,
            DayDetailedPeriodType = 2
        }

        public enum InvoiceSourceMediaTypes {
            MediaType1,
            MediaType2
        }

        [Fact]
        public void CloneInvoiceSourceObject() {
            var availSource = new InvoiceSource() {
                Identifier = "TestAvailSource",
                InvoiceGroups = new[] {
                    new InvoiceGroup {
                        Identifier = "TestGroupIdentifier",
                        Invoices = new [] {
                            new Invoice {
                                DayTimes = new [] {
                                    new InvoiceDayTime {
                                        ProgramName = "TestProgram",
                                        Days = CustomDaysOfWeek.MondayToFriday,
                                        BroadcastTimeRange = new TimeSpanRange(TimeSpan.FromHours(0), TimeSpan.FromHours(12))
                                    }
                                },
                                Name = "TestAvail",
                                OutletName = "TestOutlet",
                                Periods = new [] {
                                    new InvoicePeriod {
                                        AdjustedStartDate = new DateTime(2021, 1, 1),
                                        AdjustedEndDate = new DateTime(2021, 3, 1),
                                        AllocatedDayOfWeek = DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday,
                                        InvoicePeriodType = InvoicePeriodType.DayDetailedPeriodType,
                                        OriginalStartDate = new DateTime(2021, 1, 1),
                                        OriginalEndDate = new DateTime(2021, 3, 1),
                                        Rate = 100.0m,
                                        SpotsPerWeek = "10",
                                        DemoValues = new [] {
                                            new DemoValue {
                                                DemoCategoryRef = "TestDemoCategoryRef",
                                                DemoValueType = DemographicValueTypes.CPM,
                                                Value = "value"
                                            }
                                        },
                                        OriginalSpotsPerDayOfWeek = new ItemsPerDayOfWeek {
                                            Monday = 5,
                                            Tuesday = 3,
                                            Wednesday = 2,
                                            Thursday = 1
                                        }
                                    }
                                },
                                ProgramNames = new [] { "Program name" },
                                SourceModel = new TimeSpan(),
                                SourceModelLineNumber = 10,
                                SourceModelType = SourceModelType.SpotCableOrder,
                                SpotLength = "30",
                                UUID = "fake uuid"
                            }
                        },
                        IsPackage = true,
                        DemoCategories = new [] {
                            new InvoiceDemoCategory {
                                AgeFrom = 18,
                                AgeTo = 56,
                                DemoValueType = DemographicValueTypes.CPM,
                                Group = "TestGroup",
                                Identifier = "TestIdentifier"
                            }
                        }
                    }
                },
                MediaType = InvoiceSourceMediaTypes.MediaType2,
                OriginalProposalStartDate = new DateTime(2021, 1, 1),
                OriginalProposalEndDate = new DateTime(2021, 3, 1),
                Outlets = new[] {
                    new InvoiceOutlet {
                        Identifier = "TestOutletIdentifier",
                        MarketZone = 100,
                        MediaType = InvoiceSourceMediaTypes.MediaType2,
                        Name = "TestOutletName"
                    }
                }
            };

            // clone the original object
            var cloned = availSource.CloneX();

            Assert.Equal(availSource.Identifier, cloned.Identifier);
            Assert.Equal(availSource.MediaType, cloned.MediaType);
            Assert.Equal(availSource.OriginalProposalStartDate, cloned.OriginalProposalStartDate);
            Assert.Equal(availSource.OriginalProposalEndDate, cloned.OriginalProposalEndDate);
            Assert.Equal(availSource.InvoiceGroups.Length, cloned.InvoiceGroups.Length);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices.Length, cloned.InvoiceGroups[0].Invoices.Length);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].Name, cloned.InvoiceGroups[0].Invoices[0].Name);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].OutletName, cloned.InvoiceGroups[0].Invoices[0].OutletName);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].UUID, cloned.InvoiceGroups[0].Invoices[0].UUID);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].SourceModelType, cloned.InvoiceGroups[0].Invoices[0].SourceModelType);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].SourceModelLineNumber, cloned.InvoiceGroups[0].Invoices[0].SourceModelLineNumber);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].SpotLength, cloned.InvoiceGroups[0].Invoices[0].SpotLength);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].OutletName, cloned.InvoiceGroups[0].Invoices[0].OutletName);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].DayTimes[0].Days, cloned.InvoiceGroups[0].Invoices[0].DayTimes[0].Days);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].DayTimes[0].ProgramName, cloned.InvoiceGroups[0].Invoices[0].DayTimes[0].ProgramName);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].Periods[0].AdjustedStartDate, cloned.InvoiceGroups[0].Invoices[0].Periods[0].AdjustedStartDate);
            Assert.Equal(availSource.InvoiceGroups[0].Invoices[0].Periods[0].AdjustedEndDate, cloned.InvoiceGroups[0].Invoices[0].Periods[0].AdjustedEndDate);
            Assert.Equal(availSource.InvoiceGroups[0].StartDate, cloned.InvoiceGroups[0].StartDate);
            Assert.Equal(availSource.InvoiceGroups[0].EndDate, cloned.InvoiceGroups[0].EndDate);
            Assert.Equal(availSource.InvoiceGroups[0].DemoCategories[0].Identifier, cloned.InvoiceGroups[0].DemoCategories[0].Identifier);
            Assert.Equal(availSource.InvoiceGroups[0].DemoCategories[0].AgeFrom, cloned.InvoiceGroups[0].DemoCategories[0].AgeFrom);
            Assert.Equal(availSource.InvoiceGroups[0].DemoCategories[0].AgeTo, cloned.InvoiceGroups[0].DemoCategories[0].AgeTo);
            Assert.Equal(availSource.InvoiceGroups[0].DemoCategories[0].DemoValueType, cloned.InvoiceGroups[0].DemoCategories[0].DemoValueType);
            Assert.Equal(availSource.Outlets[0].Identifier, cloned.Outlets[0].Identifier);
            Assert.Equal(availSource.Outlets[0].Name, cloned.Outlets[0].Name);
            Assert.Equal(availSource.Outlets[0].MediaType, cloned.Outlets[0].MediaType);
            Assert.Equal(availSource.Outlets[0].MarketZone, cloned.Outlets[0].MarketZone);

            // modify some properties
            availSource.Identifier = "TestAvailSourceModified";
            availSource.InvoiceGroups[0].Identifier = "TestGroupIdentifierModified";
            availSource.InvoiceGroups[0].Invoices[0].Name = "TestAvailModified";
            availSource.InvoiceGroups[0].Invoices[0].ProgramNames[0] = "Program name modified";
            availSource.InvoiceGroups[0].Invoices[0].SourceModelType = SourceModelType.SpotCableProposal;
            availSource.InvoiceGroups[0].Invoices[0].DayTimes[0].Days = CustomDaysOfWeek.Weekend;
            availSource.Outlets[0].MarketZone = 200;
            availSource.Outlets[0].Name = "TestOutletNameModified";

            // ensure values changed are different
            Assert.NotEqual(availSource.Identifier, cloned.Identifier);
            Assert.NotEqual(availSource.InvoiceGroups[0].Identifier, cloned.InvoiceGroups[0].Identifier);
            Assert.NotEqual(availSource.InvoiceGroups[0].Invoices[0].Name, cloned.InvoiceGroups[0].Invoices[0].Name);
            Assert.NotEqual(availSource.InvoiceGroups[0].Invoices[0].ProgramNames[0], cloned.InvoiceGroups[0].Invoices[0].ProgramNames[0]);
            Assert.NotEqual(availSource.InvoiceGroups[0].Invoices[0].SourceModelType, cloned.InvoiceGroups[0].Invoices[0].SourceModelType);
            Assert.NotEqual(availSource.InvoiceGroups[0].Invoices[0].DayTimes[0].Days, cloned.InvoiceGroups[0].Invoices[0].DayTimes[0].Days);
            Assert.NotEqual(availSource.Outlets[0].MarketZone, cloned.Outlets[0].MarketZone);
            Assert.NotEqual(availSource.Outlets[0].Name, cloned.Outlets[0].Name);
        }

        #endregion

        #region Complex List Class

        public class ComplexClass {
            public int Id { get; set; }
            public string Value { get; set; }
            public decimal? Nullable { get; set; }
            public DateTime? DateTime { get; set; }
            public List<ComplexClass> List { get; set; }
            public ComplexClass[] Array { get; set; }
            public Dictionary<object, ComplexClass> Dictionary { get; set; }
            public IEnumerable<ComplexClass> IEnumerable { get; set; }
            public ComplexClass Object { get; set; }
            public object Object2 { get; set; }

            public static ComplexClass CreateTestObject() {
                var res = new ComplexClass() {
                    Id = 1,
                    Value = "Test1",
                    Nullable = 1.2m,
                    DateTime = new DateTime(2020, 1, 1),
                    List = new List<ComplexClass>() {
                        new ComplexClass() { Id = 2, Value = "Test2", List = new List<ComplexClass>() {
                            new ComplexClass() { Id = 21, Value ="Test21" },
                            new ComplexClass() { Id = 22, Value ="Test22", List = new List<ComplexClass>() {
                                new ComplexClass() { Id = 221, Value ="Test221" },
                                new ComplexClass() { Id = 222, Value ="Test222" }
                            } }
                        } },
                        new ComplexClass() { Id = 3, Value = "Test3" }

                    },
                    Array = new ComplexClass[] {
                        new ComplexClass() { Id = 4, Value = "Test4" },
                        new ComplexClass() { Id = 5, Value = "Test5" }
                    },
                    Dictionary = new Dictionary<object, ComplexClass>() {
                        { "Key1", new ComplexClass() { Id = 6, Value = "Test6" } },
                        { "Key2", new ComplexClass() { Id = 7, Value = "Test7" } }
                    },
                    Object = new ComplexClass() { Id = 8, Value = "Test8" },
                    Object2 = new ComplexClass() { Id = 9, Value = "Test9" }
                };
                var slist = new SortedList<int, ComplexClass> {
                    { 1, new ComplexClass() { Id = 8, Value = "Test8" } }
                };                
                res.IEnumerable = slist.Values;
                return res;
            }
        }


        [Fact]
        public void CopyComplexObject() {
            //Arrange
            var orig = ComplexClass.CreateTestObject();
            
            //Act
            var clone = orig.CloneX();

            //Assert            
            Assert.False(clone.Equals(orig));
            Assert.True(clone.Id == 1);
            Assert.True(clone.Value == "Test1");
            Assert.True(clone.Nullable == 1.2m);
            Assert.True(clone.DateTime == new DateTime(2020, 1, 1));
            Assert.True(clone.List.Count() == 2);
            Assert.True(clone.List.ElementAt(0).Id == 2);
            Assert.True(clone.List.ElementAt(1).Id == 3);
            Assert.True(clone.List.ElementAt(0).List.Count() == 2);
            Assert.True(clone.List.ElementAt(0).List.ElementAt(1).List.Count() == 2);
            Assert.True(clone.List.ElementAt(0).List.ElementAt(0).Id == 21);
            Assert.True(clone.List.ElementAt(0).List.ElementAt(1).List.ElementAt(0).Id == 221);
            Assert.True(clone.List != orig.List);
            Assert.True(clone.List.ElementAt(0).List != orig.List.ElementAt(0).List);
            Assert.True(clone.List.ElementAt(0).List.ElementAt(1).List != orig.List.ElementAt(0).List.ElementAt(1).List);
            Assert.True(clone.Array != orig.Array);
            Assert.True(clone.Array.Count() == 2);
            Assert.True(clone.Dictionary != orig.Dictionary);
            Assert.True(clone.Dictionary.Values.Count() == 2);
            
        }


        #endregion
    }
}
