using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Items;
using SOLITUDE.Containers;
using UnityEngine;
namespace SOLITUDE.Tests { public class AuthoringBoundaryTests {
        [Test] public void AuthoringEditsDoNotChangeRuntimeRulesOrPresentation()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var database = ScriptableObject.CreateInstance<ItemDatabase>();
            try
            {
                typeof(ItemDefinition).GetField("itemId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, "frozen");
                item.MaxStackSize = 10; item.DisplayName = "Original";
                typeof(ItemDatabase).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(database, new List<ItemDefinition> { item });
                var frozen = database.BuildRuntimeCatalog(); var presentation = database.BuildPresentationCatalog();
                item.MaxStackSize = 2; item.DisplayName = "Edited";
                Assert.IsTrue(frozen.TryGet("frozen", out var spec)); Assert.AreEqual(10, spec.MaxStackSize);
                Assert.AreEqual("Original", presentation.Resolve("frozen").DisplayName);
                var service = new ContainerCommandService(frozen); var owner = service.Register("x", 1);
                Assert.IsTrue(service.Grant(owner.Handle, "frozen", 10).Succeeded);
                Assert.AreEqual(10, owner.GetSlot(0).Stack.Quantity);
            }
            finally { UnityEngine.Object.DestroyImmediate(item); UnityEngine.Object.DestroyImmediate(database); }
        }
} }
