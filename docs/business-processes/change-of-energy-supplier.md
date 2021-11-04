# Change of energy supplier (CoS)

## Process overview

An energy supplier can, if he has consent from a consumer, request a change of energy supplier on a metering point (MP). This requires knowing either the social security number or the VAT number of the consumer currently on the metering point.

Upon receival, relevant [validation rules](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/validations/change-of-energy-supplier-validations.md) are checked. If successful, the energy supplier is registered as the future energy supplier on the MP.

This is done according to configurable parameters delimiting legal time frames for the process according to local laws.

![design](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/images/CoS_Sequence_Diagram.PNG)

Before the change of energy supplier start date it is possible for the future energy supplier to send a cancellation cancelling the request.
When a request is cancelled the request is marked as cancelled and the requesting energy supplier is removed as a future energy supplier.

![design](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/images/Cancellation_Of_CoS_Sequence_Diagram.PNG)

If an end of supply process has been registered, the change of energy supplier process changes slightly. This is described in the [end of supply documentation](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/end-of-supply.md).

## Related Integration Events

This process can result in three different integration events:

* FutureEnergySupplierChangeRegistered
* FutureEnergySupplierChangeCancelled
* EnergySupplierChanged

When a request is received with a future effective date it will result in the FutureEnergySupplierChangeRegistered integration event. Domains for which this information
is relevant will subscribe to this event.

If, before the expiration of the cancellation period, the Change of Supplier request is cancelled, this will result in the FutureEnergySupplierChangeCancelled integration event.

The change of supplier is effectuated when we reach the effective date provided in the Change of Supplier request. Upon effectuation the EnergySupplierChanged integration event is sent out.
This event is relevant for at least the following domains:

* Metering Point Domain - Tasked with sending out the MeteringPointDetails document.
* Charges Domain - Tasked with sending out the ChargeDetails document.

## Implementation details

[Link to architecture](https://github.com/Energinet-DataHub/geh-market-roles#architecture)

_NOTE: The process is not yet fully implemented. Any changes to the states will be documented when relevant._

The process manager that facilitates the entire flow has the following states:

| State                                       | Description                                                                                                                                                                                                                                                                                                                                                      |
| ------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NotStarted                                  | Flow not started                                                                                                                                                                                                                                                                                                                                                 |
| AwaitingConfirmationMessageDispatch         | Upon request receival the flow is initiated and we change our state to this first state. If it goes through our [list of validations](..\validations\change-of-energy-supplier-validations.md), this will trigger the next state and mark the energy supplier as a future energy supplier. If rejected the flow stops here and a validation report is generated. |
| AwaitingMeteringPointDetailsDispatch        | Awaiting Metering Point Master Data message to be generated and dispatched. The Metering Point domain holds this information and is tasked with generating and sending out the message. Once dispatched and dequeued the Market Roles domain will be informed hereof.                                                                                            |
| AwaitingConsumerDetailsDispatch             | Awaiting Consumer Master Data message to be generated and dispatched.                                                                                                                                                                                                                                                                                            |
| AwaitingChargeDetailsDispatch               | Awaiting Charge Details message to be generated and dispatched. The Charges domain holds this information and is tasked with generating and sending out the message. Once dispatched and dequeued the Market Roles domain will be informed hereof.                                                                                                               |
| AwaitingCurrentSupplierNotificationDispatch | Awaiting message to be generated and dispatched to notify current energy supplier of the energy supplier change.                                                                                                                                                                                                                                                 |
| AwaitingSupplierChange                      | Energy supplier change is pending.                                                                                                                                                                                                                                                                                                                               |
| Completed                                   | Future energy supplier is marked as current energy supplier and the old energy supplier is stamped with an end date. Change of energy supplier process is completed and a energy supplier changed event is raised.                                                                                                                                               |

<br/>

Whenever a message is generated it is stored in the [Outbox](http://www.kamilgrzybek.com/design/the-outbox-pattern/) table. A timer job then runs through the [Outbox](http://www.kamilgrzybek.com/design/the-outbox-pattern/) at a given interval and dispatches the message to a queue and marks the message as dispatched. The message is then picked up by the [Post Office](https://github.com/Energinet-DataHub/geh-post-office).

### Message Flow & Origin

Message flow and which domain a message originates from is depicted below:

![design](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/images/cos-message-flow.png)
