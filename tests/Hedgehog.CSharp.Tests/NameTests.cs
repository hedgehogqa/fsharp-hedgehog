using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Hedgehog.CSharp.Tests
{
    public class NameTests
    {
        private static Type[] _publicApiTypes =
            { typeof(Gen)
            , typeof(Range)
            , typeof(Property)
            };

        public static IEnumerable<object[]> AllPublicMembers()
        {
            var bindingFlags =
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly;

            foreach (var type in _publicApiTypes)
            {
                var members = type.GetMembers(bindingFlags);

                foreach (var member in members)
                {
                    // Ignore special members like indexers etc.
                    var pi = member as PropertyInfo;
                    if (pi != null && pi.IsSpecialName)
                    {
                        continue;
                    }

                    var mi = member as MethodInfo;
                    if (mi != null &&
                        (mi.IsSpecialName || mi.IsConstructor || mi.Name.StartsWith("get_")))
                    {
                        continue;
                    }

                    yield return new [] { member };
                }
            }

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
