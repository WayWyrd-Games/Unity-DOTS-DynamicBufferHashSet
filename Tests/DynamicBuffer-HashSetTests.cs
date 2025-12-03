using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

namespace com.waywyrd.dynamicbuffer_hashset.tests
{
    /// <summary>
    ///     A simple buffer element stub for testing.
    /// </summary>
    public struct TestBufferElement : IBufferElementData, IEqualityComparer<TestBufferElement>
    {
        public int Value;

        public bool Equals(TestBufferElement x, TestBufferElement y)
        {
            return x.Value == y.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public int GetHashCode(TestBufferElement obj)
        {
            return obj.Value.GetHashCode();
        }

        // Useful for convenience
        public override string ToString()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    ///     Cram all tests in here for now.
    /// </summary>
    [TestFixture]
    public class DynamicBufferHashSet_EditModeTests
    {
        private World world;
        private EntityManager entityManager;

        [SetUp]
        public void Setup()
        {
            world = new World("Test World");
            entityManager = world.EntityManager;
        }

        [TearDown]
        public void Teardown()
        {
            if (world != null && world.IsCreated)
                world.Dispose();
        }


        [Test]
        public void AddDynamicBufferHashSet_EntityHasBuffers()
        {
            var entity = entityManager.CreateEntity();
            var requestedCapacity = 4;

            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity, requestedCapacity);

            Assert.IsTrue(entityManager.HasBuffer<TestBufferElement>(entity));
            Assert.IsTrue(entityManager.HasBuffer<BufferHashSetElementData>(entity));
        }

        [Test]
        public void AddDynamicBufferHashSet_BufferHasMinCapacity()
        {
            var entity = entityManager.CreateEntity();
            var requestedCapacity = 4;

            // This performs both buffer additions
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity, requestedCapacity);

            // Re-fetch buffers after all structural changes are done
            var buffer = entityManager.GetBuffer<TestBufferElement>(entity);
            var hashBuffer = entityManager.GetBuffer<BufferHashSetElementData>(entity);

            // Capacity should be at least MinBufferCapacity
            Assert.GreaterOrEqual(buffer.Capacity, DynamicBufferHashSet.MinBufferCapacity);

            // Hash buffer length/capacity should match expectation
            Assert.GreaterOrEqual(hashBuffer.Length, DynamicBufferHashSet.MinBufferCapacity);
        }

        [Test]
        public void TryAdd_AddsElement_ContainsIsTrue()
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            var element = new TestBufferElement {Value = 10};

            var result = DynamicBufferHashSet.TryAdd(entityManager, entity, element);

            Assert.IsTrue(result);
            Assert.IsTrue(DynamicBufferHashSet.Contains(entityManager, entity, element));
        }

        [Test]
        public void TryAdd_DuplicateElement_TryAddIsFalse()
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            var element = new TestBufferElement {Value = 5};

            var result = DynamicBufferHashSet.TryAdd(entityManager, entity, element);

            Assert.IsTrue(result);

            // Add duplicate should fail and retain Length
            Assert.IsFalse(DynamicBufferHashSet.TryAdd(entityManager, entity, element));
            Assert.AreEqual(1, DynamicBufferHashSet.Length<TestBufferElement>(entityManager, entity));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public void TryAdd_Length_IsExpected(int numElements)
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity, numElements);

            for (var i = 0; i < numElements; i++)
            {
                var element = new TestBufferElement {Value = i};

                var result = DynamicBufferHashSet.TryAdd(entityManager, entity, element);
                Assert.IsTrue(result);
            }

            Assert.AreEqual(numElements, DynamicBufferHashSet.Length<TestBufferElement>(entityManager, entity));
        }

        [Test]
        public void TryAdd_WhenBufferFull_ReturnsFalse()
        {
            var entity = entityManager.CreateEntity();

            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity, 2);

            const int capacity = DynamicBufferHashSet.MinBufferCapacity;
            for (var i = 0; i < capacity; i++)
            {
                var element = new TestBufferElement {Value = i};

                // If we're not at capacity, the add should succeed
                Assert.IsTrue(DynamicBufferHashSet.TryAdd(entityManager, entity, element));
                Assert.AreEqual(i + 1, DynamicBufferHashSet.Length<TestBufferElement>(entityManager, entity));
            }

            // Now we're at capacity, this should fail and not grow the buffer
            var lastElement = new TestBufferElement {Value = capacity};
            var result = DynamicBufferHashSet.TryAdd(entityManager, entity, lastElement);

            Assert.IsFalse(result);
            Assert.AreEqual(capacity, DynamicBufferHashSet.Length<TestBufferElement>(entityManager, entity));
        }

        [Test]
        public void TryRemove_WithElement_SucceedsWithCorrectContentAndLength()
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            var a = new TestBufferElement {Value = 1};
            var b = new TestBufferElement {Value = 2};

            DynamicBufferHashSet.TryAdd(entityManager, entity, a);
            DynamicBufferHashSet.TryAdd(entityManager, entity, b);

            var removed = DynamicBufferHashSet.TryRemove(entityManager, entity, a);

            Assert.IsTrue(removed);
            Assert.IsFalse(DynamicBufferHashSet.Contains(entityManager, entity, a));
            Assert.IsTrue(DynamicBufferHashSet.Contains(entityManager, entity, b));
            Assert.AreEqual(1, DynamicBufferHashSet.Length<TestBufferElement>(entityManager, entity));
        }

        [Test]
        public void TryRemove_WithoutElement_TryRemoveFailsWithExpectedContentAndLength()
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            var a = new TestBufferElement {Value = 1};
            var notPresent = new TestBufferElement {Value = 2};

            DynamicBufferHashSet.TryAdd(entityManager, entity, a);
            var removed = DynamicBufferHashSet.TryRemove(entityManager, entity, notPresent);

            Assert.IsFalse(removed);
            Assert.IsTrue(DynamicBufferHashSet.Contains(entityManager, entity, a));
            Assert.IsFalse(DynamicBufferHashSet.Contains(entityManager, entity, notPresent));
            Assert.AreEqual(1, DynamicBufferHashSet.Length<TestBufferElement>(entityManager, entity));
        }

        [Test]
        public void TryRemove_EmptyHashSet_ReturnsFalse()
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            var element = new TestBufferElement {Value = 99};

            var removed = DynamicBufferHashSet.TryRemove(entityManager, entity, element);
            Assert.IsFalse(removed);
            Assert.AreEqual(0, DynamicBufferHashSet.Length<TestBufferElement>(entityManager, entity));
        }

        [Test]
        public void Clear_RemovesAllElementsAndHashEntries()
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            DynamicBufferHashSet.TryAdd(entityManager, entity, new TestBufferElement {Value = 1});
            DynamicBufferHashSet.TryAdd(entityManager, entity, new TestBufferElement {Value = 2});

            DynamicBufferHashSet.Clear<TestBufferElement>(entityManager, entity, 32);

            var items = entityManager.GetBuffer<TestBufferElement>(entity);
            var hash = entityManager.GetBuffer<BufferHashSetElementData>(entity);

            Assert.AreEqual(0, items.Length);
            Assert.AreEqual(0, hash.Length);
            Assert.GreaterOrEqual(items.Capacity, DynamicBufferHashSet.MinBufferCapacity);
            Assert.GreaterOrEqual(hash.Capacity, DynamicBufferHashSet.MinBufferCapacity);
        }

        private partial class TestSystem : SystemBase
        {
            protected override void OnUpdate()
            {
            }
        }

        [Test]
        public void GetDynamicBufferHashSetHandler_ReturnsValidLookups()
        {
            // Arrange
            var system = world.CreateSystemManaged<TestSystem>();
            var entity = entityManager.CreateEntity();

            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            // Act
            var handler = DynamicBufferHashSet.GetDynamicBufferHashSetHandler<TestBufferElement>(system);

            // Assert: lookups should be created and valid for this entity
            Assert.IsTrue(handler.ItemsBufferFromEntity.HasBuffer(entity));
            Assert.IsTrue(handler.HashSetBufferFromEntity.HasBuffer(entity));
        }

        [Test]
        public void TryAdd_Remove_Contains_WorkWithEntityOverloads()
        {
            var entity = entityManager.CreateEntity();
            DynamicBufferHashSet.AddDynamicBufferHashSet<TestBufferElement>(entityManager, entity);

            var elem = new TestBufferElement {Value = 42};

            var added = DynamicBufferHashSet.TryAdd(entityManager, entity, elem);
            var hasElem = DynamicBufferHashSet.Contains(entityManager, entity, elem);
            var removed = DynamicBufferHashSet.TryRemove(entityManager, entity, elem);
            var hasAfterRemove = DynamicBufferHashSet.Contains(entityManager, entity, elem);

            Assert.IsTrue(added);
            Assert.IsTrue(hasElem);
            Assert.IsTrue(removed);
            Assert.IsFalse(hasAfterRemove);
        }
    }
}
