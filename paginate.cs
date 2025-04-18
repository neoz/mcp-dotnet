namespace MCPPOC;

public class paginate
{
    // This class is used to paginate the results of a list of strings.
    // It takes a list of strings and offset to start, a page size, and returns a paginated array strings.
    public static string[] Paginate(string[] items, int offset, int pageSize)
    {
        // Check if the offset is greater than the length of the items
        if (offset >= items.Length)
        {
            return Array.Empty<string>();
        }
        
        if (pageSize== 0)
        {
            return items.Skip(offset).ToArray();
        }

        // Calculate the number of items to take
        int count = Math.Min(pageSize, items.Length - offset);
        
        // Return the paginated array
        return items.Skip(offset).Take(count).ToArray();
    }
}