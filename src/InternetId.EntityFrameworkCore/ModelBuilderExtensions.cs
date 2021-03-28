using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Text.RegularExpressions;

namespace InternetId.EntityFrameworkCore
{
    public static class ModelBuilderExtensions
    {
        private static readonly Regex pattern = new Regex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10));
        private const string replacement = "$1_$2";

        /// <summary>
        /// Converts all schema, table and property names to underscore style.
        /// Example: MySchema.MyTable.MyField becomes my_schema.my_table.my_field.
        /// Call this after all entities have been added to the <paramref name="modelBuilder"/>.
        /// </summary>
        /// <param name="modelBuilder"></param>
        public static void ApplyUnderscoreNames(this ModelBuilder modelBuilder, bool force = false)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.HasTable())
                {
                    if (entityType.GetDefaultTableName() is string defaultTableName && (defaultTableName == entityType.GetTableName() || force))
                        entityType.SetTableName(defaultTableName.Underscore());

                    if (entityType.GetDefaultSchema() is string defaultSchema && (defaultSchema == entityType.GetSchema() || force))
                        entityType.SetSchema(defaultSchema.Underscore());
                }

                var soid =
                    StoreObjectIdentifier.Create(entityType, entityType.GetSqlQuery() != null ? StoreObjectType.SqlQuery : StoreObjectType.Table) ??
                    throw new InvalidOperationException($"The {nameof(StoreObjectIdentifier)} could not be determined for {entityType.Name} entity type.");

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName(soid) ?? property.GetDefaultColumnName(soid);
                    property.SetColumnName(columnName.Underscore());
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="entityType"/> has a table.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        private static bool HasTable(this IEntityType entityType)
        {
            return
                entityType.ClrType != null &&
                entityType.BaseType == null &&
                entityType.DefiningEntityType == null &&
                entityType.GetSqlQuery() == null;
        }

        private static string Underscore(this string text)
        {
            return pattern.Replace(text, replacement).ToLowerInvariant();
        }
    }
}
