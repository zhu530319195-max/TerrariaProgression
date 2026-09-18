using System;
using Microsoft.Xna.Framework.Input;

namespace TerrariaProgression.UI;

internal readonly record struct LoadoutChord(Keys Key, bool Shift, bool Control, bool Alt)
{
    internal static bool TryParse(string? text,out LoadoutChord chord)
    {
        chord=default;
        if(string.IsNullOrWhiteSpace(text)||text.Length>80)return false;
        bool shift=false,control=false,alt=false; Keys main=Keys.None;
        foreach(string part in text.Split('+')) {
            string token=part.Trim();
            switch(token.ToLowerInvariant()) {
                case "shift": if(shift)return false;shift=true;continue;
                case "ctrl": case "control": if(control)return false;control=true;continue;
                case "alt": if(alt)return false;alt=true;continue;
            }
            if(main!=Keys.None||token.Length==0)return false;
            if(token.Length==1&&token[0] is >= '0' and <= '9')token="D"+token;
            if(!Enum.TryParse(token,true,out main)||!Enum.IsDefined(main)||main==Keys.None||char.IsDigit(token[0])
                ||main is Keys.LeftShift or Keys.RightShift or Keys.LeftControl or Keys.RightControl or Keys.LeftAlt or Keys.RightAlt or Keys.LeftWindows or Keys.RightWindows)return false;
        }
        if(main==Keys.None)return false;
        chord=new(main,shift,control,alt);return true;
    }
    internal bool Held(KeyboardState now) => now.IsKeyDown(Key)
        && (now.IsKeyDown(Keys.LeftShift)||now.IsKeyDown(Keys.RightShift))==Shift
        && (now.IsKeyDown(Keys.LeftControl)||now.IsKeyDown(Keys.RightControl))==Control
        && (now.IsKeyDown(Keys.LeftAlt)||now.IsKeyDown(Keys.RightAlt))==Alt
        && !now.IsKeyDown(Keys.LeftWindows)&&!now.IsKeyDown(Keys.RightWindows);
    internal bool Pressed(KeyboardState now,KeyboardState previous) => Held(now)&&previous.IsKeyUp(Key);
}
