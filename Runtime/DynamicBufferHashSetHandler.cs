using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace com.waywyrd.dynamicbuffer_hashset
{
    public struct DynamicBufferHashSetHandler<T> where T : unmanaged, IBufferElementData, IEqualityComparer<T>
    {
        [NativeDisableParallelForRestriction]
        public BufferLookup<T> ItemsBufferFromEntity;

        [NativeDisableParallelForRestriction]
        public BufferLookup<BufferHashSetElementData> HashSetBufferFromEntity;

        [NativeDisableParallelForRestriction]
        public DynamicBuffer<T> ItemsBuffer;

        [NativeDisableParallelForRestriction]
        public DynamicBuffer<BufferHashSetElementData> HashSetBuffer;

        public void Init(Entity entity)
        {
            Initialized = true;
            ItemsBuffer = ItemsBufferFromEntity[entity];
            HashSetBuffer = HashSetBufferFromEntity[entity];
        }

        public int Length => ItemsBuffer.Length;

        public bool Initialized { get; private set; }
    }
}