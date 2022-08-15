# frozen_string_literal: true

require 'uri'
require_relative 'spec_helper'

RSpec.describe MicrosoftKiotaAuthentication do
  it '(client credential context) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAuthentication::ClientCredentialContext.new(nil, nil, nil, {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::ClientCredentialContext.new('tenant_id', 'client_id', 'client_secret', {}) }.not_to raise_error
    expect  { MicrosoftKiotaAuthentication::ClientCredentialContext.new('', 'client_id', 'client_secret', {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::ClientCredentialContext.new('tenant_id', '', 'client_secret', {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::ClientCredentialContext.new('tenant_id', 'client_id', '', {}) }.to raise_error(StandardError)
  end

  it '(auth code context) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAuthentication::AuthorizationCodeContext.new(nil, nil, nil, nil) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::AuthorizationCodeContext.new('tenant_id', 'client_id', 'client_secret', 'redirect_uri') }.not_to raise_error
    expect  { MicrosoftKiotaAuthentication::AuthorizationCodeContext.new('', 'client_id', 'client_secret', 'redirect_uri', 'code') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::AuthorizationCodeContext.new('tenant_id', '', 'client_secret', '') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::AuthorizationCodeContext.new('tenant_id', 'client_id', '', 'redirect_uri') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::AuthorizationCodeContext.new('tenant_id', '', 'client_secret', 'redirect_uri') }.to raise_error(StandardError)
  end


  it '(on behalf of) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAuthentication::OnBehalfOfContext.new(nil, nil, nil, nil) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::OnBehalfOfContext.new('tenant_id', 'client_id', 'client_secret', 'assertion') }.not_to raise_error
    expect  { MicrosoftKiotaAuthentication::OnBehalfOfContext.new('', 'client_id', 'client_secret', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::OnBehalfOfContext.new('tenant_id', '', 'client_secret', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::OnBehalfOfContext.new('tenant_id', 'client_id', '', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthentication::OnBehalfOfContext.new('tenant_id', 'client_id', 'client_secret', '') }.to raise_error(StandardError)
  end
end
