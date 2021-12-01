# Micro-Server for microservices

[![License](https://img.shields.io/badge/LICENSE-Apache%202.0-green?style=flat)](/LICENSE)  [![Version](https://img.shields.io/badge/VERSION-RELEASE%20--%202.0-green?style=flat)](https://github.com/MixailAlexeevich/Microservices.MicroServer/releases)
### :capital_abcd: [Версия на русском языке](/README-ru.md)

This repository provides a simple server for microservices that can be used for educational purposes, as well as for business tasks with low load.

This server implements a "centralized" microservice architecture for the system.
This implies that each of the services communicates with the other through an intermediary - the server.
At first, such an organization may seem inappropriate, but it is not: centralization gives a plus in the form of simplified addressing.
Due to the fact that all services communicate through one server, they do not need to know each other's network addresses, but only need to know only the server address. This solves the NAT problem and simplifies the routing of traffic in principle.
Moreover, services do not have any unique identifier that would identify the service, so there are no difficulties with access keys and authentication here either.

## How it works: briefly

All services are equal and the same from the server's point of view. There is no difference between services.

If a service wants to transfer some data to another service, it simply sends this data to the server, marking it with a certain string.
This line can have any content, it can be formed according to any rules. This string does not have to be unique.
It is only important that the other service to which the data is intended also knows this string (or the principle of forming this string).

If the service wants to receive data from the server, then it sends a pending GET request.
This request waits for a response for some time, after which the connection is terminated and the service immediately creates a new request.
Pending requests allow you to respond as quickly as possible to the arrival of data on the server.
In the GET request, the string (mark) that was mentioned earlier is sent to the server.
If the requested string matches the marking of some data on the server, the server returns this data to the service.
If there are several data packets on the server with the same mark, they will be provided to the service one by one in the same order in which they arrived at the server.

## Composition of the repository

The repository contains the following:
* [Microservices server](/Microservices-MicroServer)
* [Service communication debugger](/MicroServer.Debugger)
* [Client on C#](/API-C%23)
* [Client on Python](/API-Python)
* [Documentation for all of the above](/Documentation/EN)

It is recommended to start the first acquaintance with the client's documentation (for example, [on С#](https://github.com/MixailAlexeevich/Microservices.MicroServer/blob/master/Documentation/EN/API-C%23.pdf)).

### Possible future changes

Depending on the need, perhaps after some time version 3.0 will be released, in which I will add hooks to the packages.
Thanks to hooks, if necessary, the service will be able to configure the interception of a data packet before this packet enters the general queue.
As a result, it turns out that there is a queue of 2 stages, where the first is processing by a hook (if any), and the second is processing by other services (non-hooks).

However, there is one small problem when introducing hooks: for the hook to work stably, the hook will need to have a certain unique identifier (namely the hook, the rest of the identifier is still not needed), which partly goes against the previously indicated "all services are equal and the same from the server's point of view".

Why do you need hooks?
Basically as a crutch that will allow some service to wedge itself into the package processing chain without making changes to other services.
It can also be useful for monitoring traffic between services. For example, to display the progress of a task to the user.

I also considered applying sorting (with lexicographic comparison) to hook IDs to create a queue of hooks.
So far, I have doubts about the need for a queue of hooks.

Now I'm not sure about the need for version 3, so if you have something to say about this, you can go [here](https://github.com/MixailAlexeevich/Microservices.MicroServer/discussions/1).