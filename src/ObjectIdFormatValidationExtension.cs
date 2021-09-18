namespace Identity.Business.Users
{
    using MongoDB.Bson;

    public static class ObjectIdFormatValidationExtension
    {
        public static bool IsValidObjectId(this string value)
        {
            try
            {
                var _ = new ObjectId(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}