using Godot;
using System;

[Tool]
public partial class CanvasText : Node2D
{
	private int _maxFontSize = 16;
	private int _dynamicFontSize;
	private string _text;
	private Vector2 _textPos = new Vector2(0, 0);
	private Vector2 _textBoxSize = new Vector2(95, 20);
	private Font _font;
	private HorizontalAlignment _alignment = HorizontalAlignment.Left;
	private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
	private Color _color = Colors.Black;

	[Export]
	public string Text
	{
		get
		{
			return _text;
		}

		set
		{
			_text = value;
			UpdateFontSize();
		}
	}

	[Export]
	public int MaxFontSize
	{
		get
		{
			return _maxFontSize;
		}

		set
		{
			_maxFontSize = value;
			UpdateFontSize();
		}
	}

	[Export]
	public Vector2 TextBoxSize
	{
		get
		{
			return _textBoxSize;
		}

		set
		{
			_textBoxSize = value;
			UpdateFontSize();
		}
	}

	[Export]
	public Font Font
	{
		get
		{
			return _font;
		}

		set
		{
			_font = value;
			UpdateFontSize();
		}
	}

	[Export]
	public HorizontalAlignment Alignment
	{
		get
		{
			return _alignment;
		}

		set
		{
			_alignment = value;
			UpdateFontSize();
		}
	}

	[Export]
	public VerticalAlignment VerticalAlignment
	{
		get
		{
			return _verticalAlignment;
		}

		set
		{
			_verticalAlignment = value;
			UpdateFontSize();
		}
	}

	[Export]
	public Color Color
	{
		get
		{
			return _color;
		}

		set
		{
			_color = value;
			UpdateFontSize();
		}
	}

	public override void _Ready()
	{
		UpdateFontSize();
	}

	public override void _Draw()
	{
		if (!string.IsNullOrEmpty(_text) && _font != null)
		{
			DrawMultilineString(
				_font,
				_textPos,
				_text,
				fontSize: _dynamicFontSize,
				alignment: _alignment,
				width: _textBoxSize.X,
				modulate: _color);
		}

		if (Engine.IsEditorHint())
		{
			DrawRect(new Rect2(Vector2.Zero, _textBoxSize), Colors.LightCyan, filled: false, width: 1);
		}
	}

	private void UpdateFontSize()
	{
		if (!string.IsNullOrEmpty(_text) && _font != null)
		{
			Vector2 textSize;
			_dynamicFontSize = _maxFontSize;
			while (true)
			{
				textSize = _font.GetMultilineStringSize(
					_text,
					fontSize: _dynamicFontSize,
					width: _textBoxSize.X);

				// Text starts above the _textPos - so substract a single line's worth of height
				// Although, for some reason it seems some of my fonts way over-estimate their height...
				var approximateFontHeight = Font.GetHeight(_dynamicFontSize) * 0.75f;

				float textY;
				if (_verticalAlignment == VerticalAlignment.Top)
				{
					textY = approximateFontHeight;
				}
				else if (_verticalAlignment == VerticalAlignment.Bottom)
				{
					textY = _textBoxSize.Y;
				}
				else
				{
					float padding = _textBoxSize.Y - approximateFontHeight;
					textY = _textBoxSize.Y - padding / 2;
				}

				_textPos = new Vector2(0, textY);

				if (textSize.X <= _textBoxSize.X && textSize.Y <= _textBoxSize.Y)
				{
					break;
				}

				_dynamicFontSize--;
				if (_dynamicFontSize <= 1)
				{
					_dynamicFontSize = 1;
					Log.Warning("Failed to find font size for ", Name, ". Text '", _text, "' does not fit in ", _textBoxSize);
					break;
				}
			}
		}
		QueueRedraw();
	}
}
