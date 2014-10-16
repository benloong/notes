

template <typename T, size_t N>
struct pool_t
{
	union chunk
	{
		chunk *next;
		char bytes[sizeof(T)];
	}

	chunk data[N]; //reserved data 
	chunk *first;

	pool_t()
	{
		for (int i = 0; i < N-1; ++i)
		{
			data[i].next = &data[i+1];
		}
		data[N-1].next = nullptr;
		first = &data[0];
	}

	T *allocate()
	{
		if (first != nullptr)
		{
		}
	}

	void free(T *p)
	{

	}
};