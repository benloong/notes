#include <iostream>
#include "matrix.hpp"

int main()
{
	Matrix4f mat;
	mat.get<3,3>() = 10.0f;
	std::cout<<mat.get<3,3>();
return 0;
}
