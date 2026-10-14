using System.Linq;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class CoreAssemblyTests
    {
        [Test]
        public void CoreAssembly_DoesNotReferenceUnityEngine()
        {
            var referenced = typeof(AssemblyMarker).Assembly.GetReferencedAssemblies();

            Assert.That(referenced.Any(a => a.Name.StartsWith("UnityEngine")), Is.False,
                "VRB.Core must stay free of UnityEngine so game logic is unit-testable and reusable for multiplayer.");
        }
    }
}
