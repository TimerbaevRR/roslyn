﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Description of a TODO comment type to find in a user's comments.
    /// </summary>
    internal readonly struct TodoCommentDescriptor
    {
        public string Text { get; }
        public int Priority { get; }

        public TodoCommentDescriptor(string text, int priority) : this()
        {
            Text = text;
            Priority = priority;
        }
    }

    internal readonly struct ParsedTodoCommentDescriptors
    {
        /// <summary>
        /// The original option text that <see cref="Descriptors"/> were parsed out of.
        /// </summary>
        public readonly string OptionText;
        public readonly ImmutableArray<TodoCommentDescriptor> Descriptors;

        public ParsedTodoCommentDescriptors(string optionText, ImmutableArray<TodoCommentDescriptor> descriptors)
        {
            this.OptionText = optionText;
            this.Descriptors = descriptors;
        }

        public static ParsedTodoCommentDescriptors Parse(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return new ParsedTodoCommentDescriptors(data, ImmutableArray<TodoCommentDescriptor>.Empty);

            var tuples = data.Split('|');
            var result = ArrayBuilder<TodoCommentDescriptor>.GetInstance();

            foreach (var tuple in tuples)
            {
                if (string.IsNullOrWhiteSpace(tuple))
                    continue;

                var pair = tuple.Split(':');

                if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[0]))
                    continue;

                if (!int.TryParse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture, out var priority))
                    continue;

                result.Add(new TodoCommentDescriptor(pair[0].Trim(), priority));
            }

            return new ParsedTodoCommentDescriptors(data, result.ToImmutableAndFree());
        }
    }
}
