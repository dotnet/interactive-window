﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    // The code here is copied from `Microsoft.VisualStudio.Text.Operations.Implementation` 
    // with minor modification to provide ability for merging undo transactions in InteractiveWindow .

    [Flags]
    internal enum TextTransactionMergeDirections
    {
        Forward = 0x0001,
        Backward = 0x0002
    }

    /// <summary>
    /// This is the merge policy used for determining whether text's undo transactions can be merged.
    /// </summary>
    internal class TextTransactionMergePolicy : IMergeTextUndoTransactionPolicy
    {
        private TextTransactionMergeDirections _allowableMergeDirections;

        public TextTransactionMergePolicy() : this(TextTransactionMergeDirections.Forward | TextTransactionMergeDirections.Backward)
        {
        }

        public TextTransactionMergePolicy(TextTransactionMergeDirections allowableMergeDirections)
        {
            _allowableMergeDirections = allowableMergeDirections;
        }

        public bool CanMerge(ITextUndoTransaction newTransaction, ITextUndoTransaction oldTransaction)
        {
            // Validate
            if (newTransaction == null)
            {
                throw new ArgumentNullException(nameof(newTransaction));
            }

            if (oldTransaction == null)
            {
                throw new ArgumentNullException(nameof(oldTransaction));
            }

            TextTransactionMergePolicy oldPolicy = oldTransaction.MergePolicy as TextTransactionMergePolicy;
            TextTransactionMergePolicy newPolicy = newTransaction.MergePolicy as TextTransactionMergePolicy;
            if (oldPolicy == null || newPolicy == null)
            {
                throw new InvalidOperationException("The MergePolicy for both transactions should be a TextTransactionMergePolicy.");
            }

            // Make sure the merge policy directions permit merging these two transactions.
            if ((oldPolicy._allowableMergeDirections & TextTransactionMergeDirections.Forward) == 0 ||
                (newPolicy._allowableMergeDirections & TextTransactionMergeDirections.Backward) == 0)
            {
                return false;
            }

            // Only merge text transactions that have the same description
            if (newTransaction.Description != oldTransaction.Description)
            {
                return false;
            }

            return true;
        }

        public void PerformTransactionMerge(ITextUndoTransaction existingTransaction, ITextUndoTransaction newTransaction)
        {
            if (existingTransaction == null)
            {
                throw new ArgumentNullException(nameof(existingTransaction));
            }
            if (newTransaction == null)
            {
                throw new ArgumentNullException(nameof(newTransaction));
            }

            // Remove trailing AfterTextBufferChangeUndoPrimitive from previous transaction and skip copying
            // initial BeforeTextBufferChangeUndoPrimitive from newTransaction, as they are unnecessary.
            int copyStartIndex = 0;

            // Copy items from newTransaction into existingTransaction.
            for (int i = copyStartIndex; i < newTransaction.UndoPrimitives.Count; i++)
            {
                existingTransaction.UndoPrimitives.Add(newTransaction.UndoPrimitives[i]);
            }
        }

        public bool TestCompatiblePolicy(IMergeTextUndoTransactionPolicy other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // Only merge transaction if they are both a text transaction
            return this.GetType() == other.GetType();
        }
    }
}
