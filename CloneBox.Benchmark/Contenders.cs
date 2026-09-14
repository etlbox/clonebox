using System.Dynamic;
using AnyClone;
using AutoMapper;
using CloneBox;
using CloneExtensions;
using Force.DeepCloner;
using Mapster;
using Newtonsoft.Json;

namespace CloneBox.Benchmark {

    internal enum Kind { Cloner, Mapper, Serializer }

    internal sealed class Contender {
        public required string Name { get; init; }
        public required Kind Kind { get; init; }
        public required long Downloads { get; init; }
        public required string Note { get; init; }
        public required Func<object, object?> Clone { get; init; }

        public bool IsCloneBox => Name == "CloneBox";
        public bool IsCloner => Kind == Kind.Cloner;
    }

    internal sealed class Result {
        public required Contender Contender { get; init; }
        public required Scenario Scenario { get; init; }
        public TimeSpan Elapsed { get; init; }
        public int Clones { get; init; }
        public long Allocated { get; init; }
        public string[] Issues { get; init; } = [];
        public string? Error { get; init; }

        public bool TimedOut => Error != null && Error.StartsWith("timed out", StringComparison.Ordinal);
        public bool Measured => Error == null && Clones > 0;
        public bool FullClone => Measured && Issues.Length == 0;
        public double MsPerClone => Elapsed.TotalMilliseconds / Math.Max(Clones, 1);
        public string FailReason => Error
            ?? (Issues.Length == 0 ? "" : Issues[0]);
    }

    internal static class Contenders {

        public static Contender[] All() => [
            CloneBox(), DeepCloner(), FastDeepCloner(), CloneExt(), AnyCloneLib(),
            MapsterLib(), AutoMapperLib(), Newtonsoft()
        ];

        static Contender CloneBox() => new() {
            Name = "CloneBox",
            Kind = Kind.Cloner,
            Downloads = -1,
            Note = "maintained, .NET 10, 280+ tests",
            Clone = CloneBoxOf
        };

        static object CloneBoxOf(object s) => s switch {
            SimplePoco p => p.CloneX(),
            ComplexGraph g => g.CloneX(),
            ExpandoObject e => e.CloneX(),
            IntoDtoCase x => x.Source.CloneXTo(IntoDtoCase.BlankTarget()),
            _ => throw new NotSupportedException(s.GetType().Name)
        };

        static Contender DeepCloner() => new() {
            Name = "DeepCloner",
            Kind = Kind.Cloner,
            Downloads = 25_755_950,
            Note = "last release 2020",
            Clone = DeepCloneOf
        };

        static object DeepCloneOf(object s) => s switch {
            SimplePoco p => p.DeepClone(),
            ComplexGraph g => g.DeepClone(),
            ExpandoObject e => e.DeepClone(),
            IntoDtoCase x => x.Source.DeepClone(),
            _ => throw new NotSupportedException(s.GetType().Name)
        };

        static Contender FastDeepCloner() => new() {
            Name = "FastDeepCloner",
            Kind = Kind.Cloner,
            Downloads = 2_810_021,
            Note = "no cycle tracking",
            Clone = s => s is IntoDtoCase x
                ? global::FastDeepCloner.DeepCloner.Clone(x.Source)
                : global::FastDeepCloner.DeepCloner.Clone(s)
        };

        static Contender CloneExt() => new() {
            Name = "CloneExtensions",
            Kind = Kind.Cloner,
            Downloads = 5_457_003,
            Note = "expression trees",
            Clone = CloneExtOf
        };

        static object CloneExtOf(object s) => s switch {
            SimplePoco p => p.GetClone(),
            ComplexGraph g => g.GetClone(),
            ExpandoObject e => e.GetClone(),
            IntoDtoCase x => x.Source.GetClone(),
            _ => throw new NotSupportedException(s.GetType().Name)
        };

        static Contender AnyCloneLib() => new() {
            Name = "AnyClone",
            Kind = Kind.Cloner,
            Downloads = 1_017_235,
            Note = "reflection",
            Clone = AnyCloneOf
        };

        static object AnyCloneOf(object s) => s switch {
            SimplePoco p => p.Clone(),
            ComplexGraph g => g.Clone(),
            ExpandoObject e => e.Clone(),
            IntoDtoCase x => x.Source.CloneTo<CustomerDto>(),
            _ => throw new NotSupportedException(s.GetType().Name)
        };

        static Contender MapsterLib() {
            var config = new TypeAdapterConfig();
            config.Default.PreserveReference(true);
            config.Compile();
            return new Contender {
                Name = "Mapster",
                Kind = Kind.Mapper,
                Downloads = 80_198_077,
                Note = "mapper, not a cloner",
                Clone = s => s is IntoDtoCase x
                    ? x.Source.Adapt<CustomerDto>(config)
                    : TypeAdapter.Adapt(s, s.GetType(), s.GetType(), config)
            };
        }

        static Contender AutoMapperLib() {
            var cfg = new MapperConfiguration(c => {
                c.CreateMap<SimplePoco, SimplePoco>();
                c.CreateMap<SimpleChild, SimpleChild>();
                c.CreateMap<ComplexGraph, ComplexGraph>().PreserveReferences();
                c.CreateMap<DataObject, DataObject>().PreserveReferences();
                c.CreateMap<ExpandoObject, ExpandoObject>();
                c.CreateMap<ExpandoObject, CustomerDto>();
            });
            var mapper = cfg.CreateMapper();
            return new Contender {
                Name = "AutoMapper",
                Kind = Kind.Mapper,
                Downloads = 1_145_508_303,
                Note = "mapper, not a cloner",
                Clone = s => s is IntoDtoCase x
                    ? mapper.Map(x.Source, IntoDtoCase.BlankTarget())
                    : mapper.Map(s, s.GetType(), s.GetType())
            };
        }

        static Contender Newtonsoft() {
            var json = new JsonSerializerSettings {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto
            };
            return new Contender {
                Name = "Newtonsoft.Json",
                Kind = Kind.Serializer,
                Downloads = 9_183_451_569,
                Note = "JSON round-trip, not a cloner",
                Clone = s => s is IntoDtoCase x
                    ? JsonConvert.DeserializeObject<CustomerDto>(JsonConvert.SerializeObject(x.Source, json), json)
                    : JsonConvert.DeserializeObject(JsonConvert.SerializeObject(s, json), s.GetType(), json)
            };
        }
    }
}
