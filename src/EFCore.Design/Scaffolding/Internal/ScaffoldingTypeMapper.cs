// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Scaffolding.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class ScaffoldingTypeMapper : IScaffoldingTypeMapper
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public ScaffoldingTypeMapper([NotNull] IRelationalTypeMappingSource typeMappingSource)
        {
            Check.NotNull(typeMappingSource, nameof(typeMappingSource));

            _typeMappingSource = typeMappingSource;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual TypeScaffoldingInfo FindMapping(
            string storeType,
            bool keyOrIndex,
            bool rowVersion)
        {
            // This is because certain providers can have no type specified as a default type e.g. SQLite
            Check.NotNull(storeType, nameof(storeType));

            var mapping = _typeMappingSource.FindMapping(storeType);
            if (mapping == null)
            {
                return null;
            }

            var canInfer = false;
            bool? scaffoldUnicode = null;
            int? scaffoldMaxLength = null;

            if (mapping.ClrType == typeof(byte[]))
            {
                // Check for inference
                var byteArrayMapping = _typeMappingSource.FindMapping(
                    typeof(byte[]),
                    keyOrIndex,
                    rowVersion: rowVersion,
                    size: mapping.Size);

                if (byteArrayMapping.StoreType.Equals(storeType, StringComparison.OrdinalIgnoreCase))
                {
                    canInfer = true;

                    // Check for size
                    var sizedMapping = _typeMappingSource.FindMapping(
                        typeof(byte[]),
                        keyOrIndex,
                        rowVersion: rowVersion);

                    scaffoldMaxLength = sizedMapping.Size != byteArrayMapping.Size ? byteArrayMapping.Size : null;
                }
            }
            else if (mapping.ClrType == typeof(string))
            {
                // Check for inference
                var stringMapping = _typeMappingSource.FindMapping(
                    typeof(string),
                    keyOrIndex,
                    unicode: mapping.IsUnicode,
                    size: mapping.Size);

                if (stringMapping.StoreType.Equals(storeType, StringComparison.OrdinalIgnoreCase))
                {
                    canInfer = true;

                    // Check for unicode
                    var unicodeMapping = _typeMappingSource.FindMapping(
                        typeof(string),
                        keyOrIndex,
                        unicode: true,
                        size: mapping.Size);

                    scaffoldUnicode = unicodeMapping.IsUnicode != stringMapping.IsUnicode ? (bool?)stringMapping.IsUnicode : null;

                    // Check for size
                    var sizedMapping = _typeMappingSource.FindMapping(
                        typeof(string),
                        keyOrIndex,
                        unicode: mapping.IsUnicode);

                    scaffoldMaxLength = sizedMapping.Size != stringMapping.Size ? stringMapping.Size : null;
                }
            }
            else
            {
                var defaultMapping = _typeMappingSource.GetMapping(mapping.ClrType);

                if (defaultMapping.StoreType.Equals(storeType, StringComparison.OrdinalIgnoreCase))
                {
                    canInfer = true;
                }
            }

            return new TypeScaffoldingInfo(
                mapping.ClrType,
                canInfer,
                scaffoldUnicode,
                scaffoldMaxLength);
        }
    }
}
