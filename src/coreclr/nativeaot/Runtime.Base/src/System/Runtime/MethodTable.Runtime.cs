// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Internal.Runtime
{
    // Extensions to MethodTable that are specific to the use in Runtime.Base.
    internal unsafe partial struct MethodTable
    {
#pragma warning disable CA1822
        internal MethodTable* GetArrayEEType()
        {
#if INPLACE_RUNTIME
            return EETypePtr.EETypePtrOf<Array>().ToPointer();
#else
            fixed (MethodTable* pThis = &this)
            {
                void* pGetArrayEEType = InternalCalls.RhpGetClasslibFunctionFromEEType(new IntPtr(pThis), ClassLibFunctionId.GetSystemArrayEEType);
                return ((delegate* <MethodTable*>)pGetArrayEEType)();
            }
#endif
        }

        internal Exception GetClasslibException(ExceptionIDs id)
        {
#if INPLACE_RUNTIME
            return RuntimeExceptionHelpers.GetRuntimeException(id);
#else
            if (IsParameterizedType)
            {
                return RelatedParameterType->GetClasslibException(id);
            }

            return EH.GetClasslibExceptionFromEEType(id, GetAssociatedModuleAddress());
#endif
        }
#pragma warning restore CA1822

        internal IntPtr GetClasslibFunction(ClassLibFunctionId id)
        {
            return (IntPtr)InternalCalls.RhpGetClasslibFunctionFromEEType((MethodTable*)Unsafe.AsPointer(ref this), id);
        }

        /// <summary>
        /// Return true if type is good for simple casting : canonical, no related type via IAT, no generic variance
        /// </summary>
        internal bool SimpleCasting()
        {
            return (_uFlags & (uint)EETypeFlags.ComplexCastingMask) == (uint)EETypeKind.CanonicalEEType;
        }

        /// <summary>
        /// Return true if both types are good for simple casting: canonical, no related type via IAT, no generic variance
        /// </summary>
        internal static bool BothSimpleCasting(MethodTable* pThis, MethodTable* pOther)
        {
            return ((pThis->_uFlags | pOther->_uFlags) & (uint)EETypeFlags.ComplexCastingMask) == 0;
        }

        internal static bool AreSameType(MethodTable* mt1, MethodTable* mt2)
        {
            if (mt1 == mt2)
                return true;

            return mt1->IsEquivalentTo(mt2);
        }

        internal bool IsEquivalentTo(MethodTable* pOtherEEType)
        {
            fixed (MethodTable* pThis = &this)
            {
                if (pThis == pOtherEEType)
                    return true;

                MethodTable* pThisEEType = pThis;

                if (pThisEEType == pOtherEEType)
                    return true;

                if (pThisEEType->IsParameterizedType && pOtherEEType->IsParameterizedType)
                {
                    return pThisEEType->RelatedParameterType->IsEquivalentTo(pOtherEEType->RelatedParameterType) &&
                        pThisEEType->ParameterizedTypeShape == pOtherEEType->ParameterizedTypeShape;
                }
            }

            return false;
        }
    }

    internal static class WellKnownEETypes
    {
        // Returns true if the passed in MethodTable is the MethodTable for System.Object
        // This is recognized by the fact that System.Object and interfaces are the only ones without a base type
        internal static unsafe bool IsSystemObject(MethodTable* pEEType)
        {
            if (pEEType->IsArray)
                return false;
            return (pEEType->NonArrayBaseType == null) && !pEEType->IsInterface;
        }

        // Returns true if the passed in MethodTable is the MethodTable for System.Array.
        // The binder sets a special CorElementType for this well known type
        internal static unsafe bool IsSystemArray(MethodTable* pEEType)
        {
            return (pEEType->ElementType == EETypeElementType.SystemArray);
        }
    }
}
