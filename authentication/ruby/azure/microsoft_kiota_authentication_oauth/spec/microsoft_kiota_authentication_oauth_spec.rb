# frozen_string_literal: true

require 'uri'
require_relative 'spec_helper'

RSpec.describe MicrosoftKiotaAuthenticationOAuth do
  it '(client credential context) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAuthenticationOAuth::ClientCredentialContext.new(nil, nil, nil, {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::ClientCredentialContext.new('tenant_id', 'client_id', 'client_secret', {}) }.not_to raise_error
    expect  { MicrosoftKiotaAuthenticationOAuth::ClientCredentialContext.new('', 'client_id', 'client_secret', {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::ClientCredentialContext.new('tenant_id', '', 'client_secret', {}) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::ClientCredentialContext.new('tenant_id', 'client_id', '', {}) }.to raise_error(StandardError)
  end

  it '(auth code context) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAuthenticationOAuth::AuthorizationCodeContext.new(nil, nil, nil, nil) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::AuthorizationCodeContext.new('tenant_id', 'client_id', 'client_secret', 'redirect_uri') }.not_to raise_error
    expect  { MicrosoftKiotaAuthenticationOAuth::AuthorizationCodeContext.new('', 'client_id', 'client_secret', 'redirect_uri', 'code') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::AuthorizationCodeContext.new('tenant_id', '', 'client_secret', '') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::AuthorizationCodeContext.new('tenant_id', 'client_id', '', 'redirect_uri') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::AuthorizationCodeContext.new('tenant_id', '', 'client_secret', 'redirect_uri') }.to raise_error(StandardError)
  end


  it '(on behalf of) throws when tenant_id/client_secret is nil/empty' do
    expect  { MicrosoftKiotaAuthenticationOAuth::OnBehalfOfContext.new(nil, nil, nil, nil) }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::OnBehalfOfContext.new('tenant_id', 'client_id', 'client_secret', 'assertion') }.not_to raise_error
    expect  { MicrosoftKiotaAuthenticationOAuth::OnBehalfOfContext.new('', 'client_id', 'client_secret', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::OnBehalfOfContext.new('tenant_id', '', 'client_secret', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::OnBehalfOfContext.new('tenant_id', 'client_id', '', 'assertion') }.to raise_error(StandardError)
    expect  { MicrosoftKiotaAuthenticationOAuth::OnBehalfOfContext.new('tenant_id', 'client_id', 'client_secret', '') }.to raise_error(StandardError)
  end

  it 'throws when Custom Context is not implemented' do
    expect { MicrosoftKiotaAuthenticationOAuth::CustomContext.new }.to raise_error(NotImplementedError)
  end
end
