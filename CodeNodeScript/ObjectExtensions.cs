using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CodeNodeScript
{
      /// <summary>
    /// object型の拡張メソッドを管理するクラス
    /// </summary>
    public static class ObjectExtensions
    {
        private const string SEPARATOR = ",";       // 区切り記号として使用する文字列
        private const string FORMAT = "{0}:{1}";    // 複合書式指定文字列
     
        /// <summary>
        /// すべての公開フィールドの情報を文字列にして返します
        /// </summary>
        public static string ToStringFields<T>(this T obj)
        {
            return string.Join(SEPARATOR, obj
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(c => string.Format(FORMAT, c.Name, c.GetValue(obj))));
        }
        
        /// <summary>
        /// すべての公開プロパティの情報を文字列にして返します
        /// </summary>
        public static string ToStringProperties<T>(this T obj)
        {
            return string.Join(SEPARATOR, obj
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(c => c.CanRead)
                .Select(c => string.Format(FORMAT, c.Name, c.GetValue(obj, null))));
        }
        
        /// <summary>
        /// すべての公開フィールドと公開プロパティの情報を文字列にして返します
        /// </summary>
        public static string[] ToStringReflection<T>(this T obj)
        {
            var joined = string.Join(SEPARATOR, 
                obj.ToStringFields(), 
                obj.ToStringProperties());
            return joined.Split( ',' );
        }
    }
      
      
}
