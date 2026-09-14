using System.Dynamic;
using CloneBox;

namespace CloneBox.Benchmark {

    public class CustomerDto {
        public int Id { get; set; }
        public string? Name { get; set; }
        public SimpleChild? Child { get; set; }
        [DoNotClone] public string? Password { get; set; }
        public string? ExtraOnTarget { get; set; }
    }

    /// <summary>
    /// Source is an ExpandoObject; the operation is CloneXTo into an existing CustomerDto.
    /// CloneBox copies matching members, skips [DoNotClone], and keeps extra fields on the target.
    /// </summary>
    public sealed class IntoDtoCase {
        public required ExpandoObject Source { get; init; }

        public static CustomerDto BlankTarget() => new() {
            ExtraOnTarget = "keep-me",
            Password = "already-set"
        };

        public static IntoDtoCase Create(int id) {
            dynamic root = new ExpandoObject();
            root.Id = id;
            root.Name = "cust-" + id;
            root.Password = "secret";
            root.Child = new SimpleChild { Id = id, Label = "child-" + id };
            return new IntoDtoCase { Source = root };
        }
    }
}
