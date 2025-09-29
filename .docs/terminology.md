# Terminology
>[!WARNING]
>ROUGH DRAFT!

* Workload Template = Bicep template defining multiple resources that make up a workload/app, maximally leaning on platform templates.
* Platform Template = Bicep template defining multiple resources that are to be used by multiple workloads and other deployments. Configurations should be focused on providing centralized services.
* Resource Template = Bicep template defining an individual resource and how to deploy it. These will typically be included in Mordos, but able to be modified to the MSPs desires. Configurations should be focused on defining best practice and business restrictions for wide-scale (multi-tenant) deployment.
* Module = Functionality add, typically included by Mordos, but able to be expanded and modified. These are code functions, like name generators and helpers.
