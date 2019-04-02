# SimpleXML
SimpleXML will be a fast and event-based SAX-Parser for C# Core (forward-only, no DOM). Goals and objectives
- almost all exceptions are to be avoided. Instead raise lots of events, which can be subscribed to optionally.
- a great level of detail to event raising => ability to parse invalid XML at the same time allowing it's detection and handling
- support for ASCII, UTF-8, UTF-16, UTF-32 character sets
- support for XML 1.0 -- including namespacess
- xml schema validation
- keep it fast and the memory footprint of the parser to 16 (or 32, with complete file in buffer) kB max. when operating with streams

No links will be followed for security reasons.
Done:
- input and buffering via stream
- speed-testing, event-based interface
