# CloneBox

**Cloning just works.**

`CloneX()` deep-clones any .NET object graph. `CloneXTo()` copies it into another type — or into an instance you already have.

[![NuGet](https://img.shields.io/nuget/v/CloneBox.svg)](https://www.nuget.org/packages/CloneBox/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net47%20%7C%20net48%20%7C%20net10.0-512bd4)](https://github.com/etlbox/clonebox)

Most “cloners” handle a flat POCO and then fall apart: cycles overflow the stack, `ExpandoObject` keeps sharing nested instances, dictionaries lose their key types. Mappers and JSON round-trips are not cloners at all.

CloneBox is built for exactly those cases, backed by **280+ tests** on .NET 10 and .NET Framework 4.8 / 4.7 — and it is the only library in the [benchmark](#benchmarks) that gets every graph right, including cloning **into an object you already have**.

---

> ### Free and open source — from the makers of ETLBox
>
> CloneBox is built and maintained by **[ETLBoxperts GmbH](https://www.etlbox.net)**, the company behind **ETLBox**, the code-first ETL and data integration library for .NET.
> We needed a cloner our own data flows could rely on, found none — so we built one and released it under the **MIT license** for the community.
> [**Discover ETLBox →**](https://www.etlbox.net)

---

## Install

```bash
dotnet add package CloneBox
```

```xml
<PackageReference Include="CloneBox" Version="*" />
```

Targets: **netstandard2.0**, **net47**, **net48**, **net10.0**.

## Usage

### Deep clone — `CloneX()`

```csharp
using CloneBox;

var copy = original.CloneX();
```

That is a real deep clone: nested objects, collections, arrays, dictionaries, and cycles. The copy does not share identity with the source.

### Clone into another object — `CloneXTo()`

`CloneXTo()` copies matching members onto a **different type** or onto an **existing instance**. Members that exist only on the target are left unchanged. `[DoNotClone]` is honored on the destination.

```csharp
public class Address {
    public string City { get; set; }
}

public class Customer {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
    public Address Address { get; set; }
}

public class CustomerDto {
    public int Id { get; set; }
    public string Name { get; set; }
    [DoNotClone] public string Password { get; set; }
    public Address Address { get; set; }
    public string ExtraOnTarget { get; set; }
}
```

Start with a filled source and a target that already has values you want to keep:

```csharp
var source = new Customer {
    Id = 1,
    Name = "Ada",
    Password = "secret",
    Address = new Address { City = "Berlin" }
};

var target = new CustomerDto {
    Password = "already-set",
    ExtraOnTarget = "keep-me"
};

source.CloneXTo(target);
```

What happened, in order:

1. **Matching members** — `Id` and `Name` are copied onto `target`. `Address` is a deep copy, not a shared reference.
2. **Extra field on the destination** — `ExtraOnTarget` is still `"keep-me"`. CloneBox does not clear members the source does not have.
3. **`[DoNotClone]` on the destination** — `Password` is still `"already-set"`. The source value `"secret"` is not written.

The same call works when source and target do not have the same shape at all.

**ExpandoObject → existing DTO**, matched by member name:

```csharp
dynamic expando = new ExpandoObject();
expando.Id = 1;
expando.Name = "Ada";
expando.Password = "secret";

var dto = new CustomerDto { Password = "already-set", ExtraOnTarget = "keep-me" };

((ExpandoObject)expando).CloneXTo(dto);
// dto.Id == 1, dto.Name == "Ada"
// dto.Password == "already-set", dto.ExtraOnTarget == "keep-me"
```

**POCO → ExpandoObject:**

```csharp
var customer = new Customer { Id = 1, Name = "Ada", Address = new Address { City = "Berlin" } };

var expando = new ExpandoObject();
customer.CloneXTo(expando);

dynamic result = expando;
// result.Id == 1, result.Name == "Ada"
// result.Address is a deep copy, not the same instance as customer.Address
```

**List → Array**, overlapping items only:

```csharp
var sourceList = new List<int> { 1, 2, 3 };
var targetArray = new int[2];

sourceList.CloneXTo(targetArray);
// targetArray is { 1, 2 }
```

> C# cannot dispatch extension methods on a `dynamic` variable. Keep the expando in an `ExpandoObject` variable (or cast it) as shown above, or call `CloneXExtensions.CloneXTo(source, target)` directly.

## Skip members

By default CloneBox copies every property and field it is allowed to see. Mark a member or a whole class with `[DoNotClone]` to leave it out — on `CloneX()` it stays at its default, on `CloneXTo()` the destination keeps its current value.

```csharp
public class User {
    public string Name { get; set; }
    [DoNotClone] public string Password { get; set; }
}

var clone = user.CloneX();
// clone.Name == user.Name, clone.Password == null
```

When you cannot change the type — a third-party class, or a rule that spans many members — use predicates instead:

```csharp
var clone = source.CloneX(new CloneSettings {
    DoNotCloneProperty = p => p.Name == "Password" || p.Name == "Token",
    DoNotCloneField    = f => f.Name.StartsWith("_cache"),
    DoNotCloneClass    = t => t == typeof(Logger)
});
```

## Settings

Defaults copy public and non-public properties and fields, and will use non-public constructors when needed. `ICloneable` is ignored unless you opt in.

```csharp
var clone = source.CloneX(new CloneSettings {
    IncludeNonPublicFields = false,
    IncludeNonPublicProperties = false,
    UseICloneableClone = true,
    Logger = logger   // Microsoft.Extensions.Logging
});
```

## What gets cloned

- Object graphs with **self-references and cycles** — no stack overflow
- **Lists, arrays** (multi-dimensional and non-zero-based included) and **dictionaries** with their runtime key types
- **`ExpandoObject` / `DynamicObject`**, including graphs that mix expandos and real classes
- Inheritance, structs, built-in types, nested collections

This is covered by **280+ xUnit tests**, run on **net10.0, net48, and net47**.

## Benchmarks

`CloneBox.Benchmark` compares CloneBox with the widely used clone libraries (DeepCloner, FastDeepCloner, CloneExtensions, AnyClone) and, as contrast, Mapster, AutoMapper 14, and a Newtonsoft.Json round-trip. Each (library × scenario) runs in an **isolated process** so a stack overflow cannot take down the suite.

Four graphs:

| # | Scenario | What it checks |
|---|----------|----------------|
| 1 | **simple** | Typed POCO, primitives, list, nested child · 1,000,000 clones |
| 2 | **cyclic** | 301 children in List/Array/Dictionary (int keys), `byte[]`, `SelfReference` · 400 clones |
| 3 | **dynamic** | `ExpandoObject` as root: typed class, nested Expando, Parent/Self cycles · 300,000 clones |
| 4 | **into** | Expando → **existing** DTO: matching members, keep extra target fields, skip `[DoNotClone]` · 200,000 runs |

Representative **Release / net10.0** run:

| Library | simple | cyclic | dynamic | into | |
|---------|-------:|-------:|--------:|-----:|--:|
| **CloneBox** | 0.35 µs | 0.33 ms | 1.92 µs | **0.91 µs** | **4/4** |
| DeepCloner | 0.20 µs | 0.37 ms | 1.18 µs | — | 3/4 |
| FastDeepCloner | 2.76 µs | — | — | — | 1/4 |
| CloneExtensions | 0.17 µs | — | — | — | 1/4 |
| AnyClone | 3.40 µs | — | — | — | 1/4 |
| Mapster | 0.36 µs | — | — | — | 1/4 |
| AutoMapper | 0.16 µs | — | — | — | 1/4 |
| Newtonsoft.Json | 2.31 µs | — | — | — | 1/4 |

Times vary by machine — reproduce with `dotnet run --project CloneBox.Benchmark -c Release`.

### How the libraries compare

The table measures two different jobs. **simple / cyclic / dynamic** are same-type deep clones; **into** is a clone onto an existing object of another type. A time is only listed when the result passed the checks — `—` means the copy was wrong or the process crashed, so speed there is meaningless.

**Same-type clone (columns 1–3).** CloneBox and DeepCloner both finish all three graphs, and DeepCloner is the faster one on the flat POCO and on Expando; on the cyclic graph they are in the same range. Everyone else breaks once the graph stops being a tree of POCOs:

| Library | What goes wrong |
|---------|-----------------|
| FastDeepCloner | no cycle tracking, so **cyclic** ends in a stack overflow |
| CloneExtensions | dictionary keys change type (`Int32` → `Object`); throws on Expando |
| AnyClone | `Parent` and the class inside the Expando are still the original instances |
| Mapster | `SelfReference` and nested Expando members still point at the source |
| AutoMapper | same as Mapster, plus it cannot bind the Expando graph at all |
| Newtonsoft.Json | dictionary keys become `string`; the nested class on the Expando is gone |

**Clone into another type (column 4).** Only CloneBox produces a correct `CustomerDto`. DeepCloner’s `DeepCloneTo` requires the destination to inherit from the source, so `ExpandoObject` → `CustomerDto` does not even compile; FastDeepCloner and CloneExtensions clone the Expando into another Expando. Mapster, AutoMapper, and Newtonsoft.Json do return a DTO-shaped object, but they build a *new* instance instead of filling the one you passed in — which is why `Password` gets overwritten and `ExtraOnTarget` is lost.

In short: DeepCloner is a solid same-type cloner and wins on simple graphs, the mappers and the serializer were never meant to be cloners, and CloneBox is the only one that is correct on cycles, on dynamic objects, **and** on clone-into.

## Who builds CloneBox

CloneBox is created and maintained by **[ETLBoxperts GmbH](https://www.etlbox.net)**, the company behind **ETLBox** — a complete ETL and data integration library for .NET.

It started as our own problem. ETLBox moves records through data flows that work with strongly typed objects *and* `ExpandoObject`, and several components have to pass on a copy of a row rather than the row itself. We went looking for a cloning library, and none of them held up against real data: cycles ended in stack overflows, dynamic objects came back sharing their nested instances, dictionaries lost their key types. So we wrote our own and hardened it with the test suite in this repository.

We are releasing it under the MIT license because a dependable deep clone is something almost every .NET project runs into sooner or later, and the community has given us plenty over the years. This is real open source, not a trial version: use it, fork it, and issues and pull requests are welcome.

If you work with data in .NET, have a look at what we build for a living: **[www.etlbox.net](https://www.etlbox.net)**. ETLBox is a code-first ETL toolbox — extract, transform and load across databases, files, APIs and streaming, with a parallel data-flow engine that handles datasets larger than memory. No GUI required, though [DirectSync](https://www.directsync.net) exists if you want one.

## License

MIT — see [LICENSE](LICENSE). Copyright ETLBoxperts GmbH.
