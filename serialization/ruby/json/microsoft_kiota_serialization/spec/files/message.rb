require 'microsoft_kiota_abstractions'
require_relative './outlook_item'

module Files
    class Message < OutlookItem
        include MicrosoftKiotaAbstractions::Parsable
        ## 
        # The fileAttachment and itemAttachment attachments for the message.
        @attachments
        @guid_id
        ## 
        # The Bcc: recipients for the message.
        @bcc_recipients
        @body
        ## 
        # The first 255 characters of the message body. It is in text format. If the message contains instances of mention, this property would contain a concatenation of these mentions as well.
        @body_preview
        ## 
        # The Cc: recipients for the message.
        @cc_recipients
        ## 
        # The ID of the conversation the email belongs to.
        @conversation_id
        ## 
        # Indicates the position of the message within the conversation.
        @conversation_index
        ## 
        # The collection of open extensions defined for the message. Nullable.
        @extensions
        @flag
        @from
        ## 
        # Indicates whether the message has attachments. This property doesn't include inline attachments, so if a message contains only inline attachments, this property is false. To verify the existence of inline attachments, parse the body property to look for a src attribute, such as <IMG src='cid:image001.jpg@01D26CD8.6C05F070'>.
        @has_attachments
        @importance
        @inference_classification
        ## 
        # A collection of message headers defined by RFC5322. The set includes message headers indicating the network path taken by a message from the sender to the recipient. It can also contain custom message headers that hold app data for the message.  Returned only on applying a $select query option. Read-only.
        @internet_message_headers
        ## 
        # The message ID in the format specified by RFC2822.
        @internet_message_id
        ## 
        # Indicates whether a read receipt is requested for the message.
        @is_delivery_receipt_requested
        ## 
        # Indicates whether the message is a draft. A message is a draft if it hasn't been sent yet.
        @is_draft
        ## 
        # Indicates whether the message has been read.
        @is_read
        ## 
        # Indicates whether a read receipt is requested for the message.
        @is_read_receipt_requested
        ## 
        # The collection of multi-value extended properties defined for the message. Nullable.
        @multi_value_extended_properties
        ## 
        # The unique identifier for the message's parent mailFolder.
        @parent_folder_id
        ## 
        # The date and time the message was received.  The date and time information uses ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z.
        @received_date_time
        ## 
        # The email addresses to use when replying.
        @reply_to
        @sender
        ## 
        # The date and time the message was sent.  The date and time information uses ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z.
        @sent_date_time
        ## 
        # The collection of single-value extended properties defined for the message. Nullable.
        @single_value_extended_properties
        ## 
        # The subject of the message.
        @subject
        ## 
        # The To: recipients for the message.
        @to_recipients
        @unique_body
        ## 
        # The URL to open the message in Outlook on the web.You can append an ispopout argument to the end of the URL to change how the message is displayed. If ispopout is not present or if it is set to 1, then the message is shown in a popout window. If ispopout is set to 0, then the browser will show the message in the Outlook on the web review pane.The message will open in the browser if you are logged in to your mailbox via Outlook on the web. You will be prompted to login if you are not already logged in with the browser.This URL cannot be accessed from within an iFrame.
        @web_link
        ## 
        ## Gets the attachments property value. The fileAttachment and itemAttachment attachments for the message.
        ## @return a attachment
        ## 
        def  attachments
            return @attachments
        end
        def guid_id
            return @guid_id
        end
        ## 
        ## Gets the bccRecipients property value. The Bcc: recipients for the message.
        ## @return a recipient
        ## 
        def  bcc_recipients
            return @bcc_recipients
        end
        ## 
        ## Gets the body property value. 
        ## @return a item_body
        ## 
        def  body
            return @body
        end
        ## 
        ## Gets the bodyPreview property value. The first 255 characters of the message body. It is in text format. If the message contains instances of mention, this property would contain a concatenation of these mentions as well.
        ## @return a string
        ## 
        def  body_preview
            return @body_preview
        end
        ## 
        ## Gets the ccRecipients property value. The Cc: recipients for the message.
        ## @return a recipient
        ## 
        def  cc_recipients
            return @cc_recipients
        end
        ## 
        ## Gets the conversationId property value. The ID of the conversation the email belongs to.
        ## @return a string
        ## 
        def  conversation_id
            return @conversation_id
        end
        ## 
        ## Gets the conversationIndex property value. Indicates the position of the message within the conversation.
        ## @return a string
        ## 
        def  conversation_index
            return @conversation_index
        end
        ## 
        ## Gets the extensions property value. The collection of open extensions defined for the message. Nullable.
        ## @return a extension
        ## 
        def  extensions
            return @extensions
        end
        ## 
        ## Gets the flag property value. 
        ## @return a followup_flag
        ## 
        def  flag
            return @flag
        end
        ## 
        ## Gets the from property value. 
        ## @return a recipient
        ## 
        def  from
            return @from
        end
        ## 
        ## Gets the hasAttachments property value. Indicates whether the message has attachments. This property doesn't include inline attachments, so if a message contains only inline attachments, this property is false. To verify the existence of inline attachments, parse the body property to look for a src attribute, such as <IMG src='cid:image001.jpg@01D26CD8.6C05F070'>.
        ## @return a boolean
        ## 
        def  has_attachments
            return @has_attachments
        end
        ## 
        ## Gets the importance property value. 
        ## @return a importance
        ## 
        def  importance
            return @importance
        end
        ## 
        ## Gets the inferenceClassification property value. 
        ## @return a inference_classification_type
        ## 
        def  inference_classification
            return @inference_classification
        end
        ## 
        ## Gets the internetMessageHeaders property value. A collection of message headers defined by RFC5322. The set includes message headers indicating the network path taken by a message from the sender to the recipient. It can also contain custom message headers that hold app data for the message.  Returned only on applying a $select query option. Read-only.
        ## @return a internet_message_header
        ## 
        def  internet_message_headers
            return @internet_message_headers
        end
        ## 
        ## Gets the internetMessageId property value. The message ID in the format specified by RFC2822.
        ## @return a string
        ## 
        def  internet_message_id
            return @internet_message_id
        end
        ## 
        ## Gets the isDeliveryReceiptRequested property value. Indicates whether a read receipt is requested for the message.
        ## @return a boolean
        ## 
        def  is_delivery_receipt_requested
            return @is_delivery_receipt_requested
        end
        ## 
        ## Gets the isDraft property value. Indicates whether the message is a draft. A message is a draft if it hasn't been sent yet.
        ## @return a boolean
        ## 
        def  is_draft
            return @is_draft
        end
        ## 
        ## Gets the isRead property value. Indicates whether the message has been read.
        ## @return a boolean
        ## 
        def  is_read
            return @is_read
        end
        ## 
        ## Gets the isReadReceiptRequested property value. Indicates whether a read receipt is requested for the message.
        ## @return a boolean
        ## 
        def  is_read_receipt_requested
            return @is_read_receipt_requested
        end
        ## 
        ## Gets the multiValueExtendedProperties property value. The collection of multi-value extended properties defined for the message. Nullable.
        ## @return a multi_value_legacy_extended_property
        ## 
        def  multi_value_extended_properties
            return @multi_value_extended_properties
        end
        ## 
        ## Gets the parentFolderId property value. The unique identifier for the message's parent mailFolder.
        ## @return a string
        ## 
        def  parent_folder_id
            return @parent_folder_id
        end
        ## 
        ## Gets the receivedDateTime property value. The date and time the message was received.  The date and time information uses ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z.
        ## @return a date_time_offset
        ## 
        def  received_date_time
            return @received_date_time
        end
        ## 
        ## Gets the replyTo property value. The email addresses to use when replying.
        ## @return a recipient
        ## 
        def  reply_to
            return @reply_to
        end
        ## 
        ## Gets the sender property value. 
        ## @return a recipient
        ## 
        def  sender
            return @sender
        end
        ## 
        ## Gets the sentDateTime property value. The date and time the message was sent.  The date and time information uses ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z.
        ## @return a date_time_offset
        ## 
        def  sent_date_time
            return @sent_date_time
        end
        ## 
        ## Gets the singleValueExtendedProperties property value. The collection of single-value extended properties defined for the message. Nullable.
        ## @return a single_value_legacy_extended_property
        ## 
        def  single_value_extended_properties
            return @single_value_extended_properties
        end
        ## 
        ## Gets the subject property value. The subject of the message.
        ## @return a string
        ## 
        def  subject
            return @subject
        end
        ## 
        ## Gets the toRecipients property value. The To: recipients for the message.
        ## @return a recipient
        ## 
        def  to_recipients
            return @to_recipients
        end
        ## 
        ## Gets the uniqueBody property value. 
        ## @return a item_body
        ## 
        def  unique_body
            return @unique_body
        end
        ## 
        ## Gets the webLink property value. The URL to open the message in Outlook on the web.You can append an ispopout argument to the end of the URL to change how the message is displayed. If ispopout is not present or if it is set to 1, then the message is shown in a popout window. If ispopout is set to 0, then the browser will show the message in the Outlook on the web review pane.The message will open in the browser if you are logged in to your mailbox via Outlook on the web. You will be prompted to login if you are not already logged in with the browser.This URL cannot be accessed from within an iFrame.
        ## @return a string
        ## 
        def  web_link
            return @web_link
        end
        ## 
        ## The deserialization information for the current model
        ## @return a i_dictionary
        ## 
        def get_field_deserializers() 
            return {
                "guidId" => lambda {|o, n| o.guid_id = n.get_guid_value() },
                "bccRecipients" => lambda {|o, n| o.bcc_recipients = n.get_collection_of_object_values(Files::Recipient) },
                "body" => lambda {|o, n| o.body = n.get_object_value(Files::ItemBody) },
                "bodyPreview" => lambda {|o, n| o.body_preview = n.get_string_value() },
                "ccRecipients" => lambda {|o, n| o.cc_recipients = n.get_collection_of_object_values(Files::Recipient) },
                "conversationId" => lambda {|o, n| o.conversation_id = n.get_string_value() },
                "conversationIndex" => lambda {|o, n| o.conversation_index = n.get_string_value() },
                "from" => lambda {|o, n| o.from = n.get_object_value(Files::Recipient) },
                "hasAttachments" => lambda {|o, n| o.has_attachments = n.get_boolean_value() },
                "internetMessageHeaders" => lambda {|o, n| o.internet_message_headers = n.get_collection_of_object_values(Files::InternetMessageHeader) },
                "internetMessageId" => lambda {|o, n| o.internet_message_id = n.get_string_value() },
                "isDeliveryReceiptRequested" => lambda {|o, n| o.is_delivery_receipt_requested = n.get_boolean_value() },
                "isDraft" => lambda {|o, n| o.is_draft = n.get_boolean_value() },
                "isRead" => lambda {|o, n| o.is_read = n.get_boolean_value() },
                "isReadReceiptRequested" => lambda {|o, n| o.is_read_receipt_requested = n.get_boolean_value() },
                "parentFolderId" => lambda {|o, n| o.parent_folder_id = n.get_string_value() },
                "receivedDateTime" => lambda {|o, n| o.received_date_time = n.get_Date_value() },
                "replyTo" => lambda {|o, n| o.reply_to = n.get_collection_of_object_values(Files::Recipient) },
                "sender" => lambda {|o, n| o.sender = n.get_object_value(Files::Recipient) },
                "sentDateTime" => lambda {|o, n| o.sent_date_time = n.get_Date_value() },
                "subject" => lambda {|o, n| o.subject = n.get_string_value() },
                "toRecipients" => lambda {|o, n| o.to_recipients = n.get_collection_of_object_values(Files::Recipient) },
                "uniqueBody" => lambda {|o, n| o.unique_body = n.get_object_value(Files::ItemBody) },
                "webLink" => lambda {|o, n| o.web_link = n.get_string_value() },
            }
        end
        ## 
        ## Serializes information the current object
        ## @param writer Serialization writer to use to serialize this model
        ## @return a void
        ## 
        def serialize(writer) 
            super.serialize(writer)
            writer.write_guid_value("guidId", @guid_id)
            writer.write_collection_of_object_values("bccRecipients", @bcc_recipients)
            writer.write_object_value("body", @body)
            writer.write_string_value("bodyPreview", @body_preview)
            writer.write_collection_of_object_values("ccRecipients", @cc_recipients)
            writer.write_string_value("conversationId", @conversation_id)
            writer.write_string_value("conversationIndex", @conversation_index)
            writer.write_collection_of_object_values("extensions", @extensions)
            writer.write_object_value("flag", @flag)
            writer.write_object_value("from", @from)
            writer.write_boolean_value("hasAttachments", @has_attachments)
            writer.write_enum_value("importance", @importance)
            writer.write_enum_value("inferenceClassification", @inference_classification)
            writer.write_collection_of_object_values("internetMessageHeaders", @internet_message_headers)
            writer.write_string_value("internetMessageId", @internet_message_id)
            writer.write_boolean_value("isDeliveryReceiptRequested", @is_delivery_receipt_requested)
            writer.write_boolean_value("isDraft", @is_draft)
            writer.write_boolean_value("isRead", @is_read)
            writer.write_boolean_value("isReadReceiptRequested", @is_read_receipt_requested)
            writer.write_collection_of_object_values("multiValueExtendedProperties", @multi_value_extended_properties)
            writer.write_string_value("parentFolderId", @parent_folder_id)
            writer.write_object_value("receivedDateTime", @received_date_time)
            writer.write_collection_of_object_values("replyTo", @reply_to)
            writer.write_object_value("sender", @sender)
            writer.write_object_value("sentDateTime", @sent_date_time)
            writer.write_collection_of_object_values("singleValueExtendedProperties", @single_value_extended_properties)
            writer.write_string_value("subject", @subject)
            writer.write_collection_of_object_values("toRecipients", @to_recipients)
            writer.write_object_value("uniqueBody", @unique_body)
            writer.write_string_value("webLink", @web_link)
        end
        ## 
        ## Sets the attachments property value. The fileAttachment and itemAttachment attachments for the message.
        ## @param value Value to set for the attachments property.
        ## @return a void
        ## 
        def  attachments=(attachments)
            @attachments = attachments
        end
        def  guid_id=(guid_id)
            @guid_id = guid_id
        end
        ## 
        ## Sets the bccRecipients property value. The Bcc: recipients for the message.
        ## @param value Value to set for the bccRecipients property.
        ## @return a void
        ## 
        def  bcc_recipients=(bccRecipients)
            @bcc_recipients = bccRecipients
        end
        ## 
        ## Sets the body property value. 
        ## @param value Value to set for the body property.
        ## @return a void
        ## 
        def  body=(body)
            @body = body
        end
        ## 
        ## Sets the bodyPreview property value. The first 255 characters of the message body. It is in text format. If the message contains instances of mention, this property would contain a concatenation of these mentions as well.
        ## @param value Value to set for the bodyPreview property.
        ## @return a void
        ## 
        def  body_preview=(bodyPreview)
            @body_preview = bodyPreview
        end
        ## 
        ## Sets the ccRecipients property value. The Cc: recipients for the message.
        ## @param value Value to set for the ccRecipients property.
        ## @return a void
        ## 
        def  cc_recipients=(ccRecipients)
            @cc_recipients = ccRecipients
        end
        ## 
        ## Sets the conversationId property value. The ID of the conversation the email belongs to.
        ## @param value Value to set for the conversationId property.
        ## @return a void
        ## 
        def  conversation_id=(conversationId)
            @conversation_id = conversationId
        end
        ## 
        ## Sets the conversationIndex property value. Indicates the position of the message within the conversation.
        ## @param value Value to set for the conversationIndex property.
        ## @return a void
        ## 
        def  conversation_index=(conversationIndex)
            @conversation_index = conversationIndex
        end
        ## 
        ## Sets the extensions property value. The collection of open extensions defined for the message. Nullable.
        ## @param value Value to set for the extensions property.
        ## @return a void
        ## 
        def  extensions=(extensions)
            @extensions = extensions
        end
        ## 
        ## Sets the flag property value. 
        ## @param value Value to set for the flag property.
        ## @return a void
        ## 
        def  flag=(flag)
            @flag = flag
        end
        ## 
        ## Sets the from property value. 
        ## @param value Value to set for the from property.
        ## @return a void
        ## 
        def  from=(from)
            @from = from
        end
        ## 
        ## Sets the hasAttachments property value. Indicates whether the message has attachments. This property doesn't include inline attachments, so if a message contains only inline attachments, this property is false. To verify the existence of inline attachments, parse the body property to look for a src attribute, such as <IMG src='cid:image001.jpg@01D26CD8.6C05F070'>.
        ## @param value Value to set for the hasAttachments property.
        ## @return a void
        ## 
        def  has_attachments=(hasAttachments)
            @has_attachments = hasAttachments
        end
        ## 
        ## Sets the importance property value. 
        ## @param value Value to set for the importance property.
        ## @return a void
        ## 
        def  importance=(importance)
            @importance = importance
        end
        ## 
        ## Sets the inferenceClassification property value. 
        ## @param value Value to set for the inferenceClassification property.
        ## @return a void
        ## 
        def  inference_classification=(inferenceClassification)
            @inference_classification = inferenceClassification
        end
        ## 
        ## Sets the internetMessageHeaders property value. A collection of message headers defined by RFC5322. The set includes message headers indicating the network path taken by a message from the sender to the recipient. It can also contain custom message headers that hold app data for the message.  Returned only on applying a $select query option. Read-only.
        ## @param value Value to set for the internetMessageHeaders property.
        ## @return a void
        ## 
        def  internet_message_headers=(internetMessageHeaders)
            @internet_message_headers = internetMessageHeaders
        end
        ## 
        ## Sets the internetMessageId property value. The message ID in the format specified by RFC2822.
        ## @param value Value to set for the internetMessageId property.
        ## @return a void
        ## 
        def  internet_message_id=(internetMessageId)
            @internet_message_id = internetMessageId
        end
        ## 
        ## Sets the isDeliveryReceiptRequested property value. Indicates whether a read receipt is requested for the message.
        ## @param value Value to set for the isDeliveryReceiptRequested property.
        ## @return a void
        ## 
        def  is_delivery_receipt_requested=(isDeliveryReceiptRequested)
            @is_delivery_receipt_requested = isDeliveryReceiptRequested
        end
        ## 
        ## Sets the isDraft property value. Indicates whether the message is a draft. A message is a draft if it hasn't been sent yet.
        ## @param value Value to set for the isDraft property.
        ## @return a void
        ## 
        def  is_draft=(isDraft)
            @is_draft = isDraft
        end
        ## 
        ## Sets the isRead property value. Indicates whether the message has been read.
        ## @param value Value to set for the isRead property.
        ## @return a void
        ## 
        def  is_read=(isRead)
            @is_read = isRead
        end
        ## 
        ## Sets the isReadReceiptRequested property value. Indicates whether a read receipt is requested for the message.
        ## @param value Value to set for the isReadReceiptRequested property.
        ## @return a void
        ## 
        def  is_read_receipt_requested=(isReadReceiptRequested)
            @is_read_receipt_requested = isReadReceiptRequested
        end
        ## 
        ## Sets the multiValueExtendedProperties property value. The collection of multi-value extended properties defined for the message. Nullable.
        ## @param value Value to set for the multiValueExtendedProperties property.
        ## @return a void
        ## 
        def  multi_value_extended_properties=(multiValueExtendedProperties)
            @multi_value_extended_properties = multiValueExtendedProperties
        end
        ## 
        ## Sets the parentFolderId property value. The unique identifier for the message's parent mailFolder.
        ## @param value Value to set for the parentFolderId property.
        ## @return a void
        ## 
        def  parent_folder_id=(parentFolderId)
            @parent_folder_id = parentFolderId
        end
        ## 
        ## Sets the receivedDateTime property value. The date and time the message was received.  The date and time information uses ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z.
        ## @param value Value to set for the receivedDateTime property.
        ## @return a void
        ## 
        def  received_date_time=(receivedDateTime)
            @received_date_time = receivedDateTime
        end
        ## 
        ## Sets the replyTo property value. The email addresses to use when replying.
        ## @param value Value to set for the replyTo property.
        ## @return a void
        ## 
        def  reply_to=(replyTo)
            @reply_to = replyTo
        end
        ## 
        ## Sets the sender property value. 
        ## @param value Value to set for the sender property.
        ## @return a void
        ## 
        def  sender=(sender)
            @sender = sender
        end
        ## 
        ## Sets the sentDateTime property value. The date and time the message was sent.  The date and time information uses ISO 8601 format and is always in UTC time. For example, midnight UTC on Jan 1, 2014 is 2014-01-01T00:00:00Z.
        ## @param value Value to set for the sentDateTime property.
        ## @return a void
        ## 
        def  sent_date_time=(sentDateTime)
            @sent_date_time = sentDateTime
        end
        ## 
        ## Sets the singleValueExtendedProperties property value. The collection of single-value extended properties defined for the message. Nullable.
        ## @param value Value to set for the singleValueExtendedProperties property.
        ## @return a void
        ## 
        def  single_value_extended_properties=(singleValueExtendedProperties)
            @single_value_extended_properties = singleValueExtendedProperties
        end
        ## 
        ## Sets the subject property value. The subject of the message.
        ## @param value Value to set for the subject property.
        ## @return a void
        ## 
        def  subject=(subject)
            @subject = subject
        end
        ## 
        ## Sets the toRecipients property value. The To: recipients for the message.
        ## @param value Value to set for the toRecipients property.
        ## @return a void
        ## 
        def  to_recipients=(toRecipients)
            @to_recipients = toRecipients
        end
        ## 
        ## Sets the uniqueBody property value. 
        ## @param value Value to set for the uniqueBody property.
        ## @return a void
        ## 
        def  unique_body=(uniqueBody)
            @unique_body = uniqueBody
        end
        ## 
        ## Sets the webLink property value. The URL to open the message in Outlook on the web.You can append an ispopout argument to the end of the URL to change how the message is displayed. If ispopout is not present or if it is set to 1, then the message is shown in a popout window. If ispopout is set to 0, then the browser will show the message in the Outlook on the web review pane.The message will open in the browser if you are logged in to your mailbox via Outlook on the web. You will be prompted to login if you are not already logged in with the browser.This URL cannot be accessed from within an iFrame.
        ## @param value Value to set for the webLink property.
        ## @return a void
        ## 
        def  web_link=(webLink)
            @web_link = webLink
        end
    end
end
