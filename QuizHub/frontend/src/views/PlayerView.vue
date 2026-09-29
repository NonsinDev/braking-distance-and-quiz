<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useQuizSignalR } from '../useQuizSignalR'

const { connect, connected, error, invoke, on } = useQuizSignalR()
const code = ref(new URLSearchParams(window.location.search).get('code') || '')
const playerName = ref('')
const joined = ref(false)
const quizTitle = ref('')
const answered = ref(false)
const timeExpired = ref(false)
const questionActive = ref(false)
const quizEnded = ref(false)
const finalScores = ref([])
const selectedOptions = ref([])
const currentQuestionIndex = ref(0)
const totalQuestions = ref(0)
const currentQuestion = ref(null)
const feedback = ref('')
const revealedPlaces = ref([])
let podiumRevealTimeoutId
let playerTimerId = null
const colors = ['red', 'blue', 'yellow', 'green']
const podiumSlots = computed(() => [1, 0, 2].map(index => ({ index, player: finalScores.value[index] })).filter(slot => slot.player))

function stopPlayerTimer() {
  if (playerTimerId) {
    clearInterval(playerTimerId)
    playerTimerId = null
  }
}

function startPlayerTimer(durationSeconds) {
  stopPlayerTimer()
  if (!durationSeconds || durationSeconds <= 0) return
  let remaining = durationSeconds
  playerTimerId = setInterval(() => {
    remaining -= 1
    if (remaining <= 0) {
      stopPlayerTimer()
      handleTimeExpired()
    }
  }, 1000)
}

function handleTimeExpired() {
  if (!answered.value) {
    timeExpired.value = true
    feedback.value = 'Zeit abgelaufen'
  }
}

onMounted(async () => {
  await connect()

  on('LobbyJoined', lobby => {
    joined.value = true
    quizTitle.value = lobby.quizTitle || 'Quiz'
  })

  on('QuestionStarted', data => {
    currentQuestion.value = data
    currentQuestionIndex.value = data.questionIndex
    totalQuestions.value = data.totalQuestions
    questionActive.value = true
    answered.value = false
    timeExpired.value = false
    selectedOptions.value = []
    feedback.value = ''
    startPlayerTimer(data.timeLimitSeconds)
  })

  on('QuestionEnded', () => {
    stopPlayerTimer()
    handleTimeExpired()
  })

  on('AnswerAccepted', ({ isCorrect, points, isPartial }) => {
    answered.value = true
    stopPlayerTimer()
    if (isCorrect) {
      feedback.value = `Richtig! +${points} P`
    } else if (isPartial) {
      feedback.value = `Teilweise richtig! +${points} P`
    } else {
      feedback.value = 'Nicht richtig.'
    }
  })

  on('QuizEnded', scores => {
    stopPlayerTimer()
    finalScores.value = scores
    quizEnded.value = true
    questionActive.value = false
    startPodiumReveal()
  })
})

function startPodiumReveal() {
  revealedPlaces.value = []
  const revealOrder = [2, 1, 0].filter(index => finalScores.value[index])
  let revealIndex = 0
  const revealNext = () => {
    if (revealIndex >= revealOrder.length) return
    revealedPlaces.value.push(revealOrder[revealIndex])
    revealIndex += 1
    if (revealIndex < revealOrder.length) podiumRevealTimeoutId = setTimeout(revealNext, 1500)
  }
  podiumRevealTimeoutId = setTimeout(revealNext, 700)
}

async function join() {
  if (!code.value || !playerName.value.trim()) return
  try {
    await invoke('JoinLobby', code.value, playerName.value)
    joined.value = true
  } catch (exception) {
    feedback.value = exception.message || 'Beitritt fehlgeschlagen.'
  }
}

async function handleOptionClick(index) {
  if (answered.value || timeExpired.value || !joined.value) return

  if (currentQuestion.value?.hasMultipleCorrectAnswers) {
    // Multi-select toggle
    const pos = selectedOptions.value.indexOf(index)
    if (pos > -1) {
      selectedOptions.value.splice(pos, 1)
    } else {
      selectedOptions.value.push(index)
    }
    if ('vibrate' in navigator) navigator.vibrate(35)
  } else {
    // Single-select: submit immediately
    selectedOptions.value = [index]
    answered.value = true
    stopPlayerTimer()
    if ('vibrate' in navigator) navigator.vibrate(80)
    await invoke('SubmitAnswer', code.value, [index])
  }
}

async function submitMultiAnswer() {
  if (answered.value || timeExpired.value || selectedOptions.value.length === 0 || !joined.value) return
  answered.value = true
  stopPlayerTimer()
  if ('vibrate' in navigator) navigator.vibrate(80)
  await invoke('SubmitAnswer', code.value, selectedOptions.value)
}

onBeforeUnmount(() => {
  stopPlayerTimer()
  if (podiumRevealTimeoutId) clearTimeout(podiumRevealTimeoutId)
})

function returnToStart() { window.location.href = window.location.pathname }
</script>

<template>
  <section class="player-shell">
    <div class="brand-mark"><span>QH</span> QUIZHUB</div>

    <!-- Login / Beitritt -->
    <div v-if="!joined" class="join-panel">
      <p class="eyebrow">TEILNEHMERZUGANG</p>
      <h1>Bereit für<br /><em>die nächste Runde?</em></h1>
      <label>Raumcode <input v-model="code" inputmode="numeric" maxlength="4" placeholder="0000" /></label>
      <label>Dein Name <input v-model="playerName" maxlength="40" placeholder="z. B. Alex" @keyup.enter="join" /></label>
      <button class="primary-action wide" :disabled="!connected" @click="join">Beitreten <span>→</span></button>
      <p v-if="error || feedback" class="error-text">{{ error || feedback }}</p>
    </div>

    <div v-else class="player-state">
      <!-- Siegerehrung -->
      <div v-if="quizEnded" class="player-results">
        <p class="eyebrow">QUIZ BEENDET</p>
        <h1>Das Siegerpodest<span>.</span></h1>
        <div class="winner-podium">
          <div
            v-for="slot in podiumSlots"
            :key="slot.player.id"
            class="winner-slot"
            :class="[`rank-${slot.index + 1}`, { revealed: revealedPlaces.includes(slot.index) }]"
          >
            <span class="winner-name">{{ slot.player.name }}</span>
            <strong>{{ slot.player.score }} P</strong>
            <b>{{ slot.index + 1 }}</b>
          </div>
        </div>
        <button class="primary-action wide" @click="returnToStart">Zur Startseite <span>→</span></button>
      </div>

      <!-- Warten auf nächste Frage -->
      <div v-else-if="!questionActive" class="waiting-state">
        <div class="pulse-dot"></div>
        <p class="eyebrow">RAUM {{ code }} · {{ quizTitle }}</p>
        <h1>Du bist dabei<span>.</span></h1>
        <p>Warte auf den Start der nächsten Frage.</p>
      </div>

      <!-- Frage aktiv -->
      <div v-else-if="currentQuestion" class="answer-state">
        <p class="eyebrow">FRAGE {{ currentQuestionIndex + 1 }} / {{ totalQuestions }}</p>

        <!-- Notification if multiple answers are correct -->
        <div v-if="currentQuestion.hasMultipleCorrectAnswers && !answered && !timeExpired" class="multi-answer-banner player-banner">
          <span class="multi-tag">MEHRFACHAUSWAHL</span>
          <span>Mehrere Antworten sind richtig! Wähle alle passenden Antworten aus.</span>
        </div>

        <h1 v-if="answered || timeExpired">{{ feedback }}</h1>
        <p v-else-if="currentQuestion.hasMultipleCorrectAnswers">Wähle alle richtigen Antworten</p>
        <p v-else>Wähle deine Antwort</p>

        <div class="answer-grid">
          <button
            v-for="(option, index) in currentQuestion.options"
            :key="index"
            class="answer-button"
            :class="[
              colors[index % 4],
              {
                checked: selectedOptions.includes(index),
                selected: selectedOptions.includes(index)
              }
            ]"
            :disabled="answered || timeExpired"
            @click="handleOptionClick(index)"
          >
            <span v-if="selectedOptions.includes(index)" class="check-indicator"></span>
            <span class="option-label">{{ ['A', 'B', 'C', 'D', 'E', 'F'][index] || index + 1 }}</span>
          </button>
        </div>

        <!-- Submit button for multi-answer questions -->
        <button
          v-if="currentQuestion.hasMultipleCorrectAnswers && !answered && !timeExpired"
          class="primary-action wide"
          :disabled="selectedOptions.length === 0"
          style="margin-top: 20px;"
          @click="submitMultiAnswer"
        >
          Antwort absenden ({{ selectedOptions.length }} gewählt) <span>→</span>
        </button>
      </div>
    </div>
  </section>
</template>
