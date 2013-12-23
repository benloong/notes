
template<typename T>
class Matrix4{
private:
	T data[16];
public:
	Matrix4(){};
    
	Matrix4(T *p){
		for(int i=0; i<16; ++i){
			data[i]= p[i];
		}
	}
    
    template <size_t N, size_t M>
    const T& get() const
    {
    	static_assert(N < 4 && M < 4,"dimension failed.");
    	return data[N*4+M];
    }

    template<size_t N, size_t M>
    T& get()
    {
    	static_assert(N < 4 && M < 4,"dimension out of range.");
    	return data[N*4+M];
    }
};

typedef Matrix4<float> Matrix4f;
