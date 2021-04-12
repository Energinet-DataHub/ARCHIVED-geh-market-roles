# Change of supplier (CoS)

## Process overview

An energy supplier can, if he has consent from a consumer, request a change of energy supplier on a metering point (MP). This requires knowing either the social security number or the VAT number of the consumer currently on the metering point.

Upon receival, relevant [validation rules](..\validations\change-of-energy-supplier-validations.md) are checked. If successful, the energy supplier is registered as the future energy supplier on the MP.

This is done according to configurable parameters delimiting legal time frames for the process according to local laws.

![design](..\images\CoS_Sequence_Diagram.PNG)

Before the change of energy supplier start date it is possible for the future energy supplier to send a cancellation cancelling the request.
When a request is cancelled the request is marked as cancelled and the requesting energy supplier is removed as a future energy supplier.

![design](..\images\Cancellation_Of_CoS_Sequence_Diagram.PNG)

If an end of supply process has been registered, the change of energy supplier process changes slightly. This is described in the [end of supply documentation](.\end-of-supply.md).

## Implementation details

[Link to architecture](https://github.com/Energinet-DataHub/geh-market-roles#architecture)

The process manager that facilitates the entire flow has the following states:

| State                                       | Description                                                                                                                                                                                                                                                                                                                                                      |
| ------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NotStarted                                  | Flow not started                                                                                                                                                                                                                                                                                                                                                 |
| AwaitingConfirmationMessageDispatch         | Upon request receival the flow is initiated and we change our state to this first state. If it goes through our [list of validations](..\validations\change-of-energy-supplier-validations.md), this will trigger the next state and mark the energy supplier as a future energy supplier. If rejected the flow stops here and a validation report is generated. |
| AwaitingMeteringPointDetailsDispatch        | Awaiting Metering Point Master Data message to be generated and dispatched.                                                                                                                                                                                                                                                                                      |
| AwaitingConsumerDetailsDispatch             | Awaiting Consumer Master Data message to be generated and dispatched.                                                                                                                                                                                                                                                                                            |
| AwaitingCurrentSupplierNotificationDispatch | Awaiting message to be generated and dispatched to notify current energy supplier of the energy supplier change.                                                                                                                                                                                                                                                 |
| AwaitingSupplierChange                      | Energy supplier change is pending.                                                                                                                                                                                                                                                                                                                               |
| Completed                                   | Future energy supplier is marked as current energy supplier and the old energy supplier is stamped with an end date. Change of energy supplier process is completed and a energy supplier changed event is raised.                                                                                                                                               |

<br/>
<br/>

Whenever a message is generated it is stored in the [Outbox](http://www.kamilgrzybek.com/design/the-outbox-pattern/) table. A timer job then runs through the [Outbox](http://www.kamilgrzybek.com/design/the-outbox-pattern/) at a given interval and dispatches the message to a queue and marks the message as dispatched. The message is then picked up by the [Post Office](https://github.com/Energinet-DataHub/geh-post-office).
