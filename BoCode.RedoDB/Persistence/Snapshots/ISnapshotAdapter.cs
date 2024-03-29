﻿using System;
using System.Threading.Tasks;
using BoCode.RedoDB.Builder;

namespace BoCode.RedoDB.Persistence.Snapshots
{
    /// <summary>
    /// This interface must be implemented by the snapshot persistence adapter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISnapshotAdapter<T> : IBuilderComponent, IDisposable
    {
        void Serialize(T redoable);
        T? Deserialize();
        Task<T?> DeserializeAsync();
        T? GetLastSnapshot();

        string? LastSnapshot { get; }
    }
}
