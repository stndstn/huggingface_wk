#pragma once

//using namespace System;

namespace DlibDn47 {
	public ref class DlibWrapper
	{
	public:
		static bool DetectFace(array<System::Byte>^ imageSrc, System::Int32^% left, System::Int32^% top, System::Int32^% right, System::Int32^% bottom);
		static System::String^ Hello(System::String^ value);

	};
}
