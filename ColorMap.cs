using System.Collections.Generic;

namespace rt
{
    public class ColorMap
    {
        private List<ushort> _from = new List<ushort>();
        private List<ushort> _to = new List<ushort>();
        private List<Color> _color = new List<Color>();

        public ColorMap Add(ushort from, ushort to, Color color)
        {
            _from.Add(from);
            _to.Add(to);
            _color.Add(color);
            return this;
        }

        public Color GetColor(ushort value)
        {
            for (int i = 0; i < _from.Count; i++)
            {
                if (_from[i] <= value && _to[i] >= value)
                {
                    return _color[i];
                }
            }
            return Color.NONE;
        }
    }
}