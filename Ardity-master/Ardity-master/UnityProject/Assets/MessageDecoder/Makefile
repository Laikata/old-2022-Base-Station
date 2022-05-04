SRCS=$(wildcard src/*.c)
OBJS=$(patsubst src/%.c, build/%.o, $(SRCS))
EXEC=build/comms

all: $(EXEC)

$(EXEC): $(OBJS)
	cc -o $@ $(OBJS)

$(OBJS): build/%.o : src/%.c
	cc -c $< -o $@

clean:
	rm build/*

run: $(EXEC)
	$(EXEC)
