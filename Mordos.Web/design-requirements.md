# Design Requirements

## Overview

This document outlines the design requirements for the Mordos Web application. It serves as a guide for developers and designers to ensure that the application meets the necessary standards and provides a consistent user experience.

## General Requirements

1. **User Authentication**: The application must support user registration and login with Entra ID. Users should be able to authenticate using their organizational accounts.
1. **Dashboard**: Users should have access to a personalized dashboard that displays relevant information and quick access to features.
1. **Content Management**: The application must allow users to create, edit, and delete content. This includes resource templates, workload templates, deployments, and more.
1. **Workload Designer**: Users should be able to design workloads using a visual interface that allows for drag-and-drop functionality. Workflow.js or nuget equivalent is recommended for this purpose.
1. **Bicep and ARM focus**: The application should support Bicep and ARM templates for resource deployment. Users should be able to create and manage these templates within the application.
1. **API Focus**: This application should rely fully on the `Mordos.API` project for backend functionality.
1. **Desired State Configuration (DSC)**: Implement DSC to ensure that the application can maintain the desired state of resources and configurations across deployments.
1. **User Roles and Permissions**: Implement a role-based access control system to manage user permissions and access to different features of the application.
1. **Multi-Tenant Support**: The application should support multi-tenant scenarios, allowing users to manage resources across different tenants.


## Feature Ideas

1. **Resource Templates**: Users should be able to create and manage resource templates that can be reused across different workloads.
1. **Workload Templates**: The application should support the creation of workload templates that can be applied to multiple tenants or environments.
1. **Deployment Management**: Users should be able to manage deployments, including viewing deployment history, status, and logs.
1. **Notifications**: Users should receive notifications for important events, such as deployment status changes, errors, or updates to resources.
