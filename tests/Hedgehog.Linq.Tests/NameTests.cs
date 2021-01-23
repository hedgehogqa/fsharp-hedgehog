using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

namespace Hedgehog.Linq.Tests
{
    public class NameTests
    {
        private static readonly Type[] _publicApiTypes =
            { typeof(Hedgehog.Linq.Gen)
            , typeof(Hedgehog.Linq.Range)
            , typeof(Hedgehog.Linq.Property)
            };

        public static IEnumerable<object[]> AllPublicMembers()
        {
            var bindingFlags =
                  BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.DeclaredOnly;

            foreach (var type in _publicApiTypes)
            {
                var members = type.GetMembers(bindingFlags);

                foreach (var member in members)
                {
                    switch (member)
                    {
                        // Ignore special members like indexers etc.
                        case PropertyInfo { IsSpecialName: true }:
                        case MethodInfo mi when NotOfInterest(mi):
                            continue;
                        default:
                            // Avoid covariant conversion from MemberInfo[] to object[].
                            yield return new object[] { member };
                            break;
                    }
                }
            }

            static bool NotOfInterest(MethodBase mi) =>
                   mi.IsSpecialName
                || mi.IsConstructor
                || mi.Name.StartsWith("get_")
                   // Static inline methods, by convention starting with '`'.
                || mi.Name.StartsWith("`");
        }

        [Theory]
        [MemberData(nameof(AllPublicMembers))]
        public void AllPublicMembersFollowDotNetNamingGuidelines(MemberInfo mi)
        {
            var startsWithUppercaseChar = char.IsUpper(mi.Name.First());
            Assert.True(startsWithUppercaseChar, $"{mi.Name} should start with uppercase letter");
        }
    }
}
