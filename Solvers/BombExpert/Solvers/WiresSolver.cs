﻿#nullable enable

using Aiml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BombExpert.Solvers {
	public class WiresSolver : ISraixService {
		private static readonly Colour[] colours = new[] { Colour.Black, Colour.Blue, Colour.Red, Colour.White, Colour.Yellow };

		private static readonly InstructionType CutWire0 = new InstructionType("wire0", "cut the first wire", (p, c, w) => Result.CutFirstWire);
		private static readonly InstructionType CutWire1 = new InstructionType("wire1", "cut the second wire", (p, c, w) => Result.CutSecondWire);
		private static readonly InstructionType CutWire2 = new InstructionType("wire2", "cut the third wire", (p, c, w) => Result.CutThirdWire);
		private static readonly InstructionType CutWire3 = new InstructionType("wire3", "cut the fourth wire", (p, c, w) => Result.CutFourthWire);
		private static readonly InstructionType CutWire4 = new InstructionType("wire4", "cut the fifth wire", (p, c, w) => Result.CutFifthWire);
		private static readonly InstructionType CutWireLast = new InstructionType("wire_last", "cut the last wire", (p, c, w) => Result.CutLastWire);
		private static readonly InstructionType CutWireColourSingle = new InstructionType("wirecolor", "cut the {0} wire", processCutWireColourFirst);
		private static readonly InstructionType CutWireColourFirst = new InstructionType("wirecolorfirst", "cut the first {0} wire", processCutWireColourFirst);
		private static readonly InstructionType CutWireColourLast = new InstructionType("wirecolorlast", "cut the last {0} wire", processCutWireColourLast);

		private static Result processCutWireColourFirst(RequestProcess p, Colour colour, Colour[] wires) {
			var index = Array.IndexOf(wires, colour);
			if (index < 0) throw new ArgumentException("No wires of the specified colour were found.");
			if (index == wires.Length - 1) return Result.CutLastWire;
			return Result.CutFirstWire + index;
		}
		private static Result processCutWireColourLast(RequestProcess p, Colour colour, Colour[] wires) {
			var index = Array.LastIndexOf(wires, colour);
			if (index < 0) throw new ArgumentException("No wires of the specified colour were found.");
			if (index == wires.Length - 1) return Result.CutLastWire;
			return Result.CutFirstWire + index;
		}

		private static ConditionType ExactlyOneColourWire => new ConditionType(nameof(ExactlyOneColourWire), "there is exactly one {0} wire", true, 1,
			(colour, wires) => wires.Count(wire => wire == colour) == 1, CutWireColourSingle);
		private static ConditionType MoreThanOneColourWire => new ConditionType(nameof(MoreThanOneColourWire), "there is more than one {0} wire", true, 2,
			(colour, wires) => wires.Count(wire => wire == colour) > 1, CutWireColourFirst, CutWireColourLast);
		private static ConditionType NoColourWire => new ConditionType(nameof(NoColourWire), "there are no {0} wires", false, 0,
			(colour, wires) => !wires.Contains(colour));
		private static ConditionType LastWireIsColour => new ConditionType(nameof(LastWireIsColour), "the last wire is {0}", true, 1,
			(colour, wires) => wires[wires.Length - 1] == colour);

		public string Process(string text, XmlAttributeCollection attributes, RequestProcess process) {
			var fields = text.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
			var ruleSeed = int.Parse(fields[0]);
			var colours = (from s in fields.Skip(1) select (Colour) Enum.Parse(typeof(Colour), s, true)).ToArray();
			var rules = GetRules(ruleSeed);

			foreach (var rule in rules[colours.Length - 3]) {
				var conditionResult = ConditionResult.FromBool(true);
				foreach (var condition in rule.Queries) {
					conditionResult = conditionResult && condition.Delegate(process, colours);
				}
				if (conditionResult) {
					var result = rule.Solution.Type.Delegate(process, rule.Solution.Colour ?? 0, colours);
					return result.ToString();
				} else if (conditionResult.Code == ConditionResultCode.Unknown) {
					return conditionResult.Details!;
				}
			}

			throw new InvalidOperationException("No rules matched?!");
		}

		public static Rule[][] GetRules(int ruleSeed) {
			if (ruleSeed == 1) {
				return new[] {
					// 3 wires
					new[] {
						new Rule(new[] { NoColourWire.GetCondition(Colour.Red) }, new Instruction(CutWire1)),
						new Rule(new[] { LastWireIsColour.GetCondition(Colour.White) }, new Instruction(CutWireLast)),
						new Rule(new[] { MoreThanOneColourWire.GetCondition(Colour.Blue) }, new Instruction(CutWireColourLast, Colour.Blue)),
						new Rule(new Instruction(CutWireLast))
					},
					// 4 wires
					new[] {
						new Rule(new[] { MoreThanOneColourWire.GetCondition(Colour.Red), Condition<Colour[]>.SerialNumberIsOdd() }, new Instruction(CutWireColourLast, Colour.Red)),
						new Rule(new[] { LastWireIsColour.GetCondition(Colour.Yellow), NoColourWire.GetCondition(Colour.Red) }, new Instruction(CutWire0)),
						new Rule(new[] { ExactlyOneColourWire.GetCondition(Colour.Blue) }, new Instruction(CutWire0)),
						new Rule(new[] { MoreThanOneColourWire.GetCondition(Colour.Yellow) }, new Instruction(CutWireLast)),
						new Rule(new Instruction(CutWire1))
					},
					// 5 wires
					new[] {
						new Rule(new[] { LastWireIsColour.GetCondition(Colour.Black), Condition<Colour[]>.SerialNumberIsOdd() }, new Instruction(CutWire3)),
						new Rule(new[] { ExactlyOneColourWire.GetCondition(Colour.Red), MoreThanOneColourWire.GetCondition(Colour.Yellow) }, new Instruction(CutWire0)),
						new Rule(new[] { NoColourWire.GetCondition(Colour.Black) }, new Instruction(CutWire1)),
						new Rule(new Instruction(CutWire0))
					},
					// 6 wires
					new[] {
						new Rule(new[] { NoColourWire.GetCondition(Colour.Yellow), Condition<Colour[]>.SerialNumberIsOdd() }, new Instruction(CutWire2)),
						new Rule(new[] { ExactlyOneColourWire.GetCondition(Colour.Yellow), MoreThanOneColourWire.GetCondition(Colour.White) }, new Instruction(CutWire3)),
						new Rule(new[] { NoColourWire.GetCondition(Colour.Red) }, new Instruction(CutWireLast)),
						new Rule(new Instruction(CutWire3))
					}
				};
			}

			var random = new MonoRandom(ruleSeed);
			var ruleSets = new Rule[4][];
			for (int i = 3; i <= 6; ++i) {
				ConditionType[][] list;
				var conditionWeights = new WeightMap<ConditionType, string>(c => c.Key);
				var instructionWeights = new WeightMap<InstructionType, string>(s => s.Key);
				var list2 = new List<Rule>();
				if (ruleSeed == 1)
					list = new[] {
						new[] { new ConditionType(Condition<Colour[]>.SerialNumberStartsWithLetter()), new ConditionType(Condition<Colour[]>.SerialNumberIsOdd()) },
						new[] { ExactlyOneColourWire, NoColourWire, LastWireIsColour, MoreThanOneColourWire }
					};
				else
					list = new[] {
						new[] { new ConditionType(Condition<Colour[]>.SerialNumberStartsWithLetter()), new ConditionType(Condition<Colour[]>.SerialNumberIsOdd()) },
						new[] { ExactlyOneColourWire, NoColourWire, LastWireIsColour, MoreThanOneColourWire },
						new[] {
							new ConditionType(Condition<Colour[]>.PortExactKey(PortType.DviD)),
							new ConditionType(Condition<Colour[]>.PortExactKey(PortType.PS2)),
							new ConditionType(Condition<Colour[]>.PortExactKey(PortType.RJ45)),
							new ConditionType(Condition<Colour[]>.PortExactKey(PortType.StereoRCA)),
							new ConditionType(Condition<Colour[]>.PortExactKey(PortType.Parallel)),
							new ConditionType(Condition<Colour[]>.PortExactKey(PortType.Serial)),
							new ConditionType(Condition<Colour[]>.EmptyPortPlate())
						}
					};

				var rules = new Rule[random.NextDouble() < 0.6 ? 3 : 4];
				ruleSets[i - 3] = rules;
				for (int j = 0; j < rules.Length; ++j) {
					var colours = new List<Colour>(WiresSolver.colours);
					var list3 = new List<Colour>();

					var conditions = new Condition<Colour[]>[random.NextDouble() < 0.6 ? 1 : 2];
					var num = i - 1;
					for (int k = 0; k < conditions.Length; ++k) {
						var possibleQueryableProperties = GetPossibleConditions(list, num, k > 0);
#if TRACE
						if (System.Diagnostics.Debugger.IsAttached) {
							System.Diagnostics.Trace.WriteLine("queryWeights:");
							foreach (var entry in conditionWeights)
								System.Diagnostics.Trace.WriteLine($"  -- {entry.Key} = {entry.Value}");
						}
#endif
						var conditionType = conditionWeights.Roll(possibleQueryableProperties, random, 0.1f);
						if (conditionType.UsesColour) {
							num -= conditionType.WiresInvolved;

							var i4 = random.Next(0, colours.Count);
							var colour = colours[i4];
							colours.RemoveAt(i4);
							if (conditionType.ColourAvailableForSolution)
								list3.Add(colour);

							conditions[k] = conditionType.GetCondition(colour);
						} else
							conditions[k] = conditionType.GetCondition(0);
					}

					var instructions = GetPossibleInstructions(i, conditions);
					Instruction solution;
					var instructionType = instructionWeights.Roll(instructions, random);
					if (list3.Count > 0)
						solution = new Instruction(instructionType, list3[random.Next(0, list3.Count)]);
					else
						solution = new Instruction(instructionType);

					var rule = new Rule(conditions, solution);
					if (ruleSeed == 1 || rule.IsValid)
						list2.Add(rule);
					else
						--j;
				}

				Utils.StableSort(list2, r => -r.Queries.Length);

				var list4 = GetPossibleInstructions(i, null);
				if (ruleSeed != 1) {
					// Remove redundant rules.
					var forbiddenId = list2[list2.Count - 1].Solution.Type.Key;
					list4.RemoveAll(l => l.Key == forbiddenId);
				}
				list2.Add(new Rule(new Instruction(random.Pick(list4), null)));
				ruleSets[i - 3] = list2.ToArray();
			}
			return ruleSets;
		}

		private static List<ConditionType> GetPossibleConditions(IList<IList<ConditionType>> lists, int wiresAvailableInQuery, bool edgeworkAllowed) {
			var list = new List<ConditionType>();
			for (var i = 0; i < lists.Count; i++) {
                for (var j = 0; j < lists[i].Count; j++) {
					var conditionType = lists[i][j];
                    if ((edgeworkAllowed || conditionType.UsesColour) && conditionType.WiresInvolved <= wiresAvailableInQuery)
						list.Add(conditionType);
				}
			}

			return list;
		}

        private static List<InstructionType> GetPossibleInstructions(int wireCount, IEnumerable<Condition<Colour[]>>? conditions) {
			var list = new List<InstructionType>();
			list.Add(CutWire0);
			list.Add(CutWire1);
			list.Add(CutWireLast);
			if (wireCount >= 4) list.Add(CutWire2);
			if (wireCount >= 5) list.Add(CutWire3);
			if (wireCount >= 6) list.Add(CutWire4);

			if (conditions != null) {
				foreach (var condition in conditions) {
					if (condition is WireCondition wireCondition && wireCondition.Type.AdditionalSolutions != null)
						list.AddRange(wireCondition.Type.AdditionalSolutions);
				}
			}

			return list;
		}

		private bool TryGetSerialNumberIsOdd(RequestProcess process, out bool result) {
			if (!process.User.Predicates.TryGetValue("BombSerialNumberIsOdd", out var predicate) && predicate != "unknown") {
				result = false;
				return false;
			}
			result = bool.Parse(predicate);
			return true;
		}

		public enum Result {
			CutFirstWire,
			CutSecondWire,
			CutThirdWire,
			CutFourthWire,
			CutFifthWire,
			CutLastWire
		}

		public class InstructionType {
			public delegate Result SolutionDelegate(RequestProcess process, Colour conditionColour, Colour[] wireColours);
			public string Key { get; }
			public string Text { get; }
			public SolutionDelegate Delegate { get; }

			public InstructionType(string key, string text, SolutionDelegate solutionDelegate) {
				this.Key = key;
				this.Text = text;
				this.Delegate = solutionDelegate;
			}

			public override string ToString() => $"{{{this.Key}}}";
		}

		/// <summary>Represents a class of conditions, which can be used to construct conditions by providing a wire colour.</summary>
		public class ConditionType {
			public string Key { get; }
			public bool UsesColour { get; }
			public bool ColourAvailableForSolution { get; }
			public int WiresInvolved { get; }
			public InstructionType[] AdditionalSolutions { get; }
			public Func<Colour, Condition<Colour[]>> Delegate { get; }

			/// <summary>Initialises a colour-dependent <see cref="ConditionType"/> with the specified properties.</summary>
			/// <param name="key">The condition identifier.</param>
			/// <param name="text">The manual description of the solution. <c>{0}</c> is replaced with the colour.</param>
			/// <param name="colourAvailableForSolution">If true, the provided colour may be used in the instruction.</param>
			/// <param name="wiresInvolved">Always true.</param>
			/// <param name="func">A delegate that creates the condition from the colour.</param>
			/// <param name="additionalSolutions">A list of instructions that may be used in a rule with a condition of this type.</param>
			public ConditionType(string key, string text, bool colourAvailableForSolution, int wiresInvolved, Func<Colour, Colour[], bool> func, params InstructionType[] additionalSolutions) {
				this.Key = key;
				this.UsesColour = true;
				this.ColourAvailableForSolution = colourAvailableForSolution;
				this.WiresInvolved = wiresInvolved;
				this.AdditionalSolutions = additionalSolutions;
				this.Delegate = c => new WireCondition(this, string.Format(text, c.ToString().ToLower()), c, (p, w) => ConditionResult.FromBool(func(c, w)));
			}
			/// <summary>Initialises a <see cref="ConditionType"/> that produces the specified condition independent of colour.</summary>
			public ConditionType(Condition<Colour[]> condition) {
				this.Key = condition.Key;
				this.Delegate = c => condition;
				this.AdditionalSolutions = new InstructionType[0];
			}

			public Condition<Colour[]> GetCondition(Colour colour) => this.Delegate(colour);

			public override string ToString() => $"{{{this.Key}}}";
		}

		public class WireCondition : Condition<Colour[]> {
			public Colour Colour { get; }
			public ConditionType Type { get; }

			public WireCondition(ConditionType type, string text, Colour colour, ConditionDelegate conditionDelegate) : base(type.Key, text, conditionDelegate) {
				this.Type = type;
				this.Colour = colour;
			}
		}

		public struct Instruction {
			public InstructionType Type { get; }
			public Colour? Colour { get; }

			public Instruction(InstructionType type) : this(type, null) { }
			public Instruction(InstructionType type, Colour? colour) {
				this.Type = type;
				this.Colour = colour;
			}

			public override string ToString() => string.Format(this.Type.Text, this.Colour);
		}

		public class Rule {
			public Condition<Colour[]>[] Queries { get; }
			public Instruction Solution { get; }

			public Rule(Instruction solution) : this(new Condition<Colour[]>[0], solution) { }
			public Rule(Condition<Colour[]>[] queries, Instruction solution) {
				this.Queries = queries;
				this.Solution = solution;
			}

			private bool Has(string conditionKey, Colour colour)
				=> this.Queries.Any(c => c is WireCondition c2 && c2.Key == conditionKey && c2.Colour == colour);

			public bool IsValid {
				get {
					if (this.Queries.Length == 1) return true;

					for (int i = 0; i < 4 /* sic */; ++i) {
						var colour = colours[i];
						var count = 0;
						// There may not be more than one 'colour' query for the same colour.
						foreach (var query in this.Queries) {
							if ((query.Key == nameof(ExactlyOneColourWire) || query.Key == nameof(NoColourWire) || query.Key == nameof(MoreThanOneColourWire))
								&& ((WireCondition) query).Colour == colour)
								++count;
						}
						if (count >= 2) return false;

						if (this.Has(nameof(LastWireIsColour), colour)) {
							// There may not be a 'last wire is <colour>' and 'there are no <colour> wires' clauses for the same colour.
							if (this.Has(nameof(NoColourWire), colour)) return false;
							// There may not be more than one 'last wire is <colour>' clause for different colours.
							for (var j = i + 1; j < 5; ++j) {
								if (this.Has(nameof(LastWireIsColour), colours[j]))
									return false;
							}
						}
					}

					return true;
				}
			}

			public override string ToString()
				=> (this.Queries.Length > 0 ? "if " + string.Join(" and ", (IEnumerable<Condition<Colour[]>>) this.Queries) + ", " : "") + this.Solution.ToString();
		}
	}
}
