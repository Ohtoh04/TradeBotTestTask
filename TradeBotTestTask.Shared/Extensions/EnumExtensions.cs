
using System.ComponentModel;
using System.Reflection;

namespace TradeBotTestTask.Shared.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        Type type = value.GetType();
        MemberInfo[] memberInfo = type.GetMember(value.ToString());

        if (memberInfo.Length > 0)
        {
            object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrs.Length > 0)
            {
                return ((DescriptionAttribute)attrs[0]).Description;
            }
        }

        return value.ToString();
    }
}
