import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { onBeforeUnmount, ref } from 'vue'

const hubUrl = import.meta.env.VITE_HUB_URL || '/quizHub'

export function useQuizSignalR() {
  const connection = ref(null)
  const connected = ref(false)
  const error = ref('')

  async function connect() {
    if (connection.value) return
    const hub = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
      .build()

    hub.onreconnecting(() => { connected.value = false })
    hub.onreconnected(() => { connected.value = true })
    hub.onclose(() => { connected.value = false })
    connection.value = hub

    try {
      await hub.start()
      connected.value = true
      error.value = ''
    } catch (exception) {
      error.value = 'Die Verbindung zum Quizserver konnte nicht hergestellt werden.'
      connection.value = null
      throw exception
    }
  }

  async function invoke(method, ...args) {
    if (!connection.value) await connect()
    return connection.value.invoke(method, ...args)
  }

  function on(eventName, handler) {
    connection.value?.on(eventName, handler)
  }

  function off(eventName, handler) {
    connection.value?.off(eventName, handler)
  }

  onBeforeUnmount(async () => {
    if (connection.value) await connection.value.stop()
  })

  return { connection, connected, error, connect, invoke, on, off }
}
