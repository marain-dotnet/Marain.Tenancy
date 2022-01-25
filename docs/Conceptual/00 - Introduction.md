# Multi-tenanted solution architecture
In this document, we’re going to discuss concepts in multi-tenanted solution architecture.

We will talk about
- what multi-tenanted means
- when to choose a multi-tenanted architecture
- tenancy as a hierarchy
- the management plane and the solution plane
- how tenancy impacts application code and resource choices
- how Corvus.Tenancy and Marain.Tenancy help with building multi-tenanted services.

## What does multi-tenanted mean? 
Let’s start by thinking about the distinction between *multi-tenant* and *multi-user*.

The chief problem with giving multiple users access to a computer is that we need a fair way of ensuring that they don’t tread on each other’s toes.

Initially, people used to book time on the computer. They would get exclusive access to a giant room of glowing valves, for a specific period of time. Then get physically booted out.

By the 1970s, the slightly confusingly named [time sharing](https://en.wikipedia.org/wiki/Time-sharing) model predominated. Multiple users could access a single computer at the same time, sharing the resources.

The security and fair-shares implications of this ultimately led to the development of isolated process models, virtual machines, containers, and the cloud/edge computing model we know today. 

As part of this shift, the line between “computer user” and “solution user” has also blurred.

In a modern web application, for example, there is frequently a single logical “solution” serving hundreds, thousands, or millions of concurrent users.

That solution is likely implemented using many compute and storage resources, distributed across various physical and virtual machines, but it is conceptually *one solution, lots of users*. To developer and user alike, it looks like a single set of resources, serving many users. Those users may work together in logical groups (like teams, or organizations) but they are fundamentally sharing a single solution.

A multi-tenanted solution, on the other hand, can be thought of as *lots of solutions, lots of users*. To the *owner* of the multi-tenanted solution, it looks conceptually like *many isolated solutions*, each of which independently serves many users. To any individual tenant, their “tenancy” looks like a single set of resources serving their users, and no-one else’s.

[multi-user v. multi tenant](multi-user v multi-tenant.png)

## When should a solution be multi-tenanted, not multi-user?
A multi-tenanted solution is required when one or more groups of users have a strong requirement for *isolation* from other groups of users.

Typically this isolation will manifest in one or more of the following ways:
1. The ability for a tenant to provision, manage, and monitor a collection of resources independently of other tenants.
1. The ability for a tenant to bring their own resources (including, for example, application code, configuration, data, cryptographic keys, policy) and integrate into their solution
1. The ability to provide particular kinds of security boundary around their solution and control identity and access (including the ability to deny access to the entity that provides the tenant with those resources)
1. The inability for one tenant to determine the existence or otherwise of another tenant.

Note that this is not simply *data isolation*. Most data storage solutions (blob storage, SQL databases etc.) have mechanisms that allow you to control access to subsets of the data, constrained to particular identities. For example, techniques like Row Level Security (RLS), filtering, and data partitioning provide various types of security and access boundary. Multi-user systems will often use these techniques to ensure segregation of user data.

A multi-tenanted system will also use these techniques to provide data segregation, but adds in other constraints from the list above. 

## Tenancy as a hierarchy
It is also very common for a multi-tenanted solution to itself be used to provide other multi-tenanted solutions. This can be a source of considerable confusion.

For example, the Microsoft Azure platform is a multi-tenanted solution. 

An Azure tenant has resources that are isolated in all the ways described above, and we can use those resources to build solutions which are themselves multi-tenanted.

With that in mind, you can think of tenancy as a tree, with a *root tenant* which owns the multi-tenanted solution. This root tenant has zero or more *child tenants* for which it provides some (other) services.

[Tenant hierarchies](Tenant hierarchies.png)

Why *zero* or more children? Well – a root tenant with no children is our traditional single tenant application.

### Do we live in a root universe?
The isolation rules for tenancy mean that from the perspective of the solution deployed in that tenant it is irrelevant that any given tenant has a parent (which may itself be parent...and so on...). 

So, any given child tenant can be thought of as the *root of its own tenancy hierarchy*.

Ultimately, there may well be a real “root” tenant somewhere in the tree, but a good tenancy model means we should not have to care whether we are the ultimate root tenant, or some child of a child of a child.

[Tenant roots](Tenant roots.png)

For example, as an Azure customer, we don’t need to think about the fact that Azure has other tenants when we use it to provision and manage our own resources. Nor do we need to theorise about the way in which services like Cosmos DB or SQL Serverless are themselves provisioned and managed as tenants in Azure when we provision and consume resources from them.

Should we wish to provide a multi-tenancy model for our solution, we are free to do so in any way we choose. We are not bound to the tenancy model of our parent.

A common challenge when designing a multi-tenanted system is thinking about the right “scope”. Am I thinking about the solution in my tenant (which should not expose details of tenancy) or the multi-tenancy solution itself (which should not depend on implementation details of the solutions that happen to be deployed as its tenants)? You must take care that one does not bleed into the other.

## Solution plane and management plane
There is one respect in which the child tenant is aware of the existence of a parent. It needs to be able to *manage its tenancy*. To do so, it will provision, configure, and monitor services through tools provided by the parent that hosts it.

We often refer to the divide between the services you *consume in your tenant* and the services you use to *manage your tenant* as the *solution plane* and the *management plane*.

[Solution v. Management Plane](Solution v Management Plane)

Careful conceptual separation of solution plane and management plane is essential.

## Common requirements of a multi-tenanted solution
Let’s think about a very generalised application. We might say that a typical application has a stimulus/response design like this:

1. Receive a stimulus of some kind (e.g. HTTP request, event, message, timer)
1. Connect to and authenticate against one or more services using service-specific credentials, in order to obtain the information required to respond to the stimulus.
1. Process that information (and optionally respond), possibly connecting to other services to update state related to that request.

There are numerous services you can use with that basic pattern – HTTP requests, queues and message busses, event stores, various flavours of databases and storage, caching. The details of these services is outside the scope of this discussion, but they are all consumed using this basic pattern.

In a multi-tenanted solution, you need to layer a notion of “the intended tenant” into your design:

1. Receive a stimulus of some kind, and dispatch it to the appropriate tenant
1. Connect to a service using the instance and credentials appropriate for the given tenant
1. Process that information (and optionally respond) using code and configuration appropriate for that tenant, possibly connecting to the correct instance of other services to update state related to that request, using the instance and credentials appropriate for that tenant.

The way in which this is done is very technology specific, but in essence the environment hosting the tenancy needs to:

1. Identify the intended tenant for the stimulus
1. Identify the code that is needed to handle the stimulus, and the compute resource to which that stimulus should be directed
1. Provide a means for that code to find and connect to the services it uses.

There are common patterns that can help a multi-tenanted solution perform these tasks, and abstract as much of this from the code executing in the tenant as possible.

Typically this will include
1. code at the stimulus/event routing layer (e.g. http routing, message handlers)
1. factories to be used by the client code to create tenant-specific instances of service clients, configured appropriately

## Solution-layer concerns
As we dig down in the stack, we finally reach implementation concerns in the multi-tenanted solution itself.

How do you design a SQL Azure database to host multi-tenanted data? What are the isolation characteristics? What about Blob storage? Cosmos DB? 

These solution-level design choices are well explored in the Azure documentation for each individual service. For example
- [Azure SQL Database]( https://docs.microsoft.com/en-us/azure/azure-sql/database/saas-tenancy-app-design-patterns)
- [Cosmos DB]( https://docs.microsoft.com/en-us/azure/architecture/guide/multitenant/service/cosmos-db)
- [Azure Storage]( https://docs.microsoft.com/en-us/azure/architecture/guide/multitenant/service/storage)

When reading that documentation, it is important to consider whether it describes multi-tenant or multi-user scenarios, as we have defined them.
They sometimes use terms such as *hostile* and *non-hostile* tenants (respectively), to distinguish between these models.

In general, the Azure documentation describes isolation mechanisms for a particular compute or storage service, to support different tenancy and user isolation models, rather than considering a more holistic/solution view of tenancy, bringing together a variety of services into a multi-tenanted solution.

It gives you the nuts-and-bolts (we will talk more about this below), but you will need to understand how each service you consume slots into your overall tenancy model.

For any given technology or service, there are implementation choices which trade-off a number of factors:

1. Security
    - Does a breach of this service or malicious code execution in one tenant offer a mechanism to hop-over into another tenant?
1. Noisy neighbours
    - Does the operation of one tenant have performance or scalability implications for the operation of another tenant? Can one tenant deny or degrade service to others?
1. Scalability and density
    - What are the scalability characteristics of the solution as an individual tenant scales up, and/or the number of tenants scales up?
1. Portability
    - Can a tenant be moved (transparently) from one physical resource to another to resolve conflicts and/or as the tenant scales/buys more resources?
1. Cost
    - What is the cost model for the resource?
1. Deployment and Manageability
    - What are the deployment and manageability implications as the number of tenants scales up? What are the observability and monitoring characteristics for a an individual tenant? For the root tenant?
1. Versioning and updates
    - Are there implications for versioning or updating tenanted solutions? What about the cost of updating the management plane for those services?

## Shared infrastructure v. isolated infrastructure
What about higher-level SaaS services like messaging, document creation, CRM, or telephony? What happens when we aggregate services outside our tenancy host?

The approach to multi-tenancy for those depends somewhat on the architecture of the service providers themselves. Can you reflect your own tenancy hierarchy into those services, or do you treat them as a single shared resource consumed by all your tenants? Even if you can reflect that tenancy hierarchy, should you do so?

This is actually a common decision you need to make for every resource you consume in your multi-tenanted solution. Should we use shared infrastructure or isolated infrastructure?

With shared infrastructure, compute and storage requirements for different tenants are dispatched in common services. Isolation is provided by features within those common services.

For example, you might have a single SQL Server Database instance shared between multiple tenants, whose data is partitioned using Row Level Security. Or a service which abstracts a single instance of Twilio dispatching all the text messages on behalf of all tenants using common credentials owned the parent tenant provider.

With isolated infrastructure, each tenant gets its own service instances.

For example each tenant might get its own SQL Server Database Instance, or even its own SQL Server instance. Or a service which dispatches text messages to a particular Twilio account on behalf of a particular tenant, using credentials specific to that tenant.

The ultimate form of this isolated infrastructure is “bring your own service”. In this case, a multi-tenanted provider allows you to configure their solution with your own instances of particular storage or compute infrastructure, encryption keys, credentials etc.

For example, a tenant might configure the text-message dispatch service provided by its parent to use the tenant’s own Twilio account, giving it access to a key in KeyVault that contains their Twilio credentials, configured with access rights appropriate for the service.

## Corvus.Tenancy and Marain.Tenancy
To help with implementing multi-tenanted solutions using the dotnet platform, we maintain two open-source projects: [Corvus.Tenancy]( https://github.com/corvus-dotnet/Corvus.Tenancy) and [Marain.Tenancy](https://github.com/marain-dotnet/Marain.Tenancy).

### Management plane
In Corvus.Tenancy, we provide low-level services for creating and managing Tenants as logical entities within your multi-tenanted solution. It also gives individual tenants within that solution the ability to manage features of their own tenancy and tenanted-service configuration.

Marain.Tenancy then exposes these low-level services as APIs for you to develop your own multi-tenanted solutions “as a service”.
You would use these APIs to develop your own management plane for your multi-tenanted solution.

We also provide tooling and recommended practices to assist in the deployment and management of multi-tenanted solutions using this model. These are found in [Marain.Instance](https://github.com/marain-dotnet/Marain.Instance).

### Solution plane
Corvus.Tenancy also provides libraries for popular storage, identity, secret management, and compute solutions, like Azure Storage, Cosmos DB, SQL Server and Key Vault. It leverages conventions over tenancy configuration to create appropriate instances of the types in the client SDKs for those technologies, pre-configured for your application’s tenant.

## Summary
In this document, we’ve looked at aspects of multi-tenanted solution architecture. We talked about
- multi-tenanted v. multi-user solutions
- the isolation goals of a multi-tenanted model
- when to choose a multi-tenanted architecture
- the hierarchy of multi-tenanted solutions in multi-tenanted solutions
- separation of the management plane and the solution plane
- data and compute segregation strategies, including shared infrastructure, isolated infrastructure, and “bring your own service”
- how Corvus.Tenancy and Marain.Tenancy help with building multi-tenanted services.

## Next steps
•	[Listomatic– an overview of a multi-tenanted reference solution using Marain.Tenancy](https://github.com/marain-dotnet/Marain.List-O-Matic)
