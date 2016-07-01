using System.Collections.Generic;
using System.IO;

namespace StrangeSoft.WildStar.Database
{
    public class WildstarTableRow
    {
        public IReadOnlyList<WildstarTableColumn> Columns { get; private set; }

        public static WildstarTableRow Load(TableHeader header, List<TableFieldDescriptor> descriptors, int rowNumber,
            BinaryReader reader)
        {
            List<WildstarTableColumn> columns = new List<WildstarTableColumn>();
            foreach (var column in descriptors)
            {
                var dataColumn = WildstarTableColumn.Load(header, rowNumber, column, reader);
                columns.Add(dataColumn);
            }

            return new WildstarTableRow
            {
                Columns = columns
            };
        }
    }
}