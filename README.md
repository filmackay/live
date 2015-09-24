# Live

Throttled streams for structured data, and continuous queries.

This is an old .NET project (2013) that has been, somewhat brutally, upgraded to the latest version of RX - and placed on Github.

It creates the concept of Live scalars, lists, sets etc. and allows queries to be bound to them. Distinct from event processing (RX), Live is all about state. It does not focus on delivering every change, but instead minimising latency and delivering the latest information. In that vein, it optimises refresh rates in order to achieve the latest information possible - making it appropriate for highly volatile data such as some financial markets applications.
