﻿using System;
using System.ComponentModel;

namespace SimpleMock
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IInvisibleSystemObjectMethods
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        Type GetType();

        [EditorBrowsable(EditorBrowsableState.Never)]
        string ToString();
    }
}
