namespace Kiota.Builder.Tests.OpenApiSampleFiles;


public static class GithubRepos
{
    public static readonly string OpenApiYaml = @"
openapi: 3.0.0
info:
  title: GitHub API
  description: API for managing GitHub organizations and repositories
  version: 1.0.0
  contact:
    name: GitHub API Support
    url: https://support.github.com/contact
    email: support@github.com
servers:
  - url: https://api.github.com
paths:
  /orgs/{org}/repos:
    post:
      description: >
        Creates a new repository in the specified organization. The authenticated user must be a member of the organization.

        **OAuth scope requirements**

        When using [OAuth](https://docs.github.com/apps/building-oauth-apps/understanding-scopes-for-oauth-apps/), authorizations must include:

        *   `public_repo` scope or `repo` scope to create a public repository. Note: For GitHub AE, use `repo` scope to create an internal repository.
        *   `repo` scope to create a private repository
      externalDocs:
        description: API method documentation
        url: https://docs.github.com/rest/reference/repos#create-an-organization-repository
      operationId: repos/create-in-org
      parameters:
        - $ref: '#/components/parameters/org'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required:
                - name
              properties:
                allow_auto_merge:
                  type: boolean
                  default: false
                  description: Either `true` to allow auto-merge on pull requests, or `false` to disallow auto-merge.
                delete_branch_on_merge:
                  type: boolean
                  default: false
                  description: >
                    Either `true` to allow automatically deleting head branches when pull requests are merged, or `false` to prevent automatic deletion.
                    **The authenticated user must be an organization owner to set this property to `true`.**
                description:
                  type: string
                  description: A short description of the repository.
                gitignore_template:
                  type: string
                  description: >
                    Desired language or platform [.gitignore template](https://github.com/github/gitignore) to apply.
                    Use the name of the template without the extension. For example, ""Haskell"".
                has_downloads:
                  type: boolean
                  default: true
                  description: Whether downloads are enabled.
                merge_commit_message:
                  type: string
                  enum:
                    - PR_BODY
                    - PR_TITLE
                    - BLANK
                  description: >
                    The default value for a merge commit message.
                    - `PR_TITLE` - default to the pull request's title.
                    - `PR_BODY` - default to the pull request's body.
                    - `BLANK` - default to a blank commit message.
                squash_merge_commit_message:
                  type: string
                  enum:
                    - PR_BODY
                    - COMMIT_MESSAGES
                    - BLANK
                  description: >
                    The default value for a squash merge commit message:
                    - `PR_BODY` - default to the pull request's body.
                    - `COMMIT_MESSAGES` - default to the branch's commit messages.
                    - `BLANK` - default to a blank commit message.
                squash_merge_commit_title:
                  type: string
                  enum:
                    - PR_TITLE
                    - COMMIT_OR_PR_TITLE
                  description: >
                    The default value for a squash merge commit title:
                    - `PR_TITLE` - default to the pull request's title.
                    - `COMMIT_OR_PR_TITLE` - default to the commit's title (if only one commit) or the pull request's title (when more than one commit).
                team_id:
                  type: integer
                  description: >
                    The id of the team that will be granted access to this repository. This is only valid when creating a repository in an organization.
                use_squash_pr_title_as_default:
                  type: boolean
                  default: false
                  deprecated: true
                  description: >
                    Either `true` to allow squash-merge commits to use pull request title, or `false` to use commit message.
                    **This property has been deprecated. Please use `squash_merge_commit_title` instead.
                visibility:
                  type: string
                  enum:
                    - public
                    - private
                  description: The visibility of the repository.
      responses:
        '201':
          description: Response
          content:
            application/json:
              examples:
                default:
                  $ref: '#/components/examples/repository'
              schema:
                $ref: '#/components/schemas/repository'
          headers:
            Location:
              schema:
                type: string
              example: 'https://api.github.com/repos/octocat/Hello-World'
        '403':
          $ref: '#/components/responses/forbidden'
        '422':
          $ref: '#/components/responses/validation_failed'
      summary: Create an organization repository
      tags:
        - repos
      x-github:
        category: repos
        enabledForGitHubApps: true
        githubCloudOnly: false
        subcategory: null
components:
  parameters:
    org:
      name: org
      in: path
      description: Organization name
      required: true
      schema:
        type: string
  responses:
    forbidden:
      description: Forbidden
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/basic-error'
    validation_failed:
      description: Validation failed, or the endpoint has been spammed.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/validation-error'
  schemas:
    basic-error:
      title: Basic Error
      description: Basic Error
      type: object
      properties:
        documentation_url:
          type: string
        message:
          type: string
        status:
          type: string
        url:
          type: string
    repository:
      title: Repository
      description: Repository
      type: object
      properties:
        archived:
          type: boolean
        assignees_url:
          type: string
        branches_url:
          type: string
        collaborators_url:
          type: string
        full_name:
          type: string
        git_url:
          type: string
        labels_url:
          type: string
        language:
          type: string
        releases_url:
          type: string
        score:
          type: integer
        size:
          type: integer
      example: |
        full_name: octocat/Hello-World
        private: false
        owner:
          login: octocat
          id: 1
          node_id: MDQ6VXNlcjE=
          avatar_url: 'https://github.com/images/error/octocat_happy.gif'
          gravatar_id: ''
          url: 'https://api.github.com/users/octocat'
          html_url: 'https://github.com/octocat'
          followers_url: 'https://api.github.com/users/octocat/followers'
          following_url: 'https://api.github.com/users/octocat/following{/other_user}'
          gists_url: 'https://api.github.com/users/octocat/gists{/gist_id}'
          starred_url: 'https://api.github.com/users/octocat/starred{/owner}{/repo}'
          subscriptions_url: 'https://api.github.com/users/octocat/subscriptions'
          organizations_url: 'https://api.github.com/users/octocat/orgs'
          repos_url: 'https://api.github.com/users/octocat/repos'
          events_url: 'https://api.github.com/users/octocat/events{/privacy}'
          received_events_url: 'https://api.github.com/users/octocat/received_events'
          type: User
          site_admin: false
        html_url: 'https://github.com/octocat/Hello-World'
        description: This is your first repository
        fork: false
        url: 'https://api.github.com/repos/octocat/Hello-World'
        created_at: '2011-01-26T19:01:12Z'
        updated_at: '2011-01-26T19:14:43Z'
        pushed_at: '2011-01-26T19:06:43Z'
        homepage: 'https://github.com'
        size: 100
        stargazers_count: 80
        watchers_count: 80
        language: JavaScript
        has_issues: true
        has_projects: true
        has_downloads: true
        has_wiki: true
        has_pages: false
        forks_count: 9
        mirror_url: null
        archived: false
        disabled: false
        open_issues_count: 0
        license:
          key: mit
          name: MIT License
          spdx_id: MIT
          url: 'https://api.github.com/licenses/mit'
          node_id: MDc6TGljZW5zZTEz
        forks: 9
        open_issues: 0
        watchers: 80
        default_branch: master
    validation-error:
      title: Validation Error
      description: Validation Error
      type: object
      required:
        - message
        - documentation_url
      properties:
        documentation_url:
          type: string
        errors:
          type: array
          items:
            type: object
            required:
              - code
            properties:
              code:
                type: string
              field:
                type: string
              index:
                type: integer
              message:
                type: string
              resource:
                type: string
              value:
                oneOf:
                  - type: string
                    nullable: true
                  - type: integer
                    nullable: true
                  - type: array
                    items:
                      type: string
                    nullable: true
        message:
          type: string
      example:
        documentation_url: 'https://developer.github.com/v3/activity/events/types/#watchevent'
        message: 'You need to be signed in to watch a repository'
    ";

}
