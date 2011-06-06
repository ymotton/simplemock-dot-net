SimpleMock-dot-net
==================

SimpleMock is a [mocking framework][1] that is designed to be lightweight, simplistic, minimalistic, strongly typed and generally very easy to use via a Fluent API. It is inspired by [Moq][2] and was implemented as an academic challenge rather than being an alternative to the more popular and mature mocking frameworks out there.

Method Mocking
--------------
    var mock = new Mock<IFoo>();
	
	// Define a mock implementation for the Add(int, int) method to return 3 in case arguments 1 and 2 are provided.
	mock.HasMethod(foo => foo.Add(1, 2))
	    .Returns(3);

    // Define a mock implementation for the ToString(int) method to return "1" in case argument 1 is provided.
	mock.HasMethod(foo => foo.ToString(1))
	    .Returns("1");

    // It supports a callback handler to be executed any time a mock implementation is ran
	mock.HasMethod(foo => foo.SomeMethod("SomeParameter"))
	    .Returns("SomeResult")
		.Subscribe(() => Console.WriteLine("SomeMethod() is executed"));

    // It also supports providing a custom implementation so you can make a sort of inline implementation
	// Note: the arguments are ignored as they are only used for overload resolution
	mock.HasMethod(
            foo => foo.MyComplexMethod(1, 2, 3),
            (a, b, c) => 
            {
                return a + b + c;
            });

[1]: http://en.wikipedia.org/wiki/Mock_object
[2]: http://code.google.com/p/moq/wiki/QuickStart